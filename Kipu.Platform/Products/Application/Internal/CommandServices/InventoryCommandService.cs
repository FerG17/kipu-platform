using Cortex.Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Errors;
using Kipu.Platform.Products.Domain.Model.Events;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Products.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.CommandServices;

/// <summary>
///     Handles inventory mutations — the operations that change how much
///     stock a product has, always leaving a StockMovement audit trail.
/// </summary>
public class InventoryCommandService(
    IInventoryItemRepository inventoryItemRepository,
    IStockMovementRepository stockMovementRepository,
    IBatchRepository batchRepository,
    IProductRepository productRepository,
    IWarehouseRepository warehouseRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IValidator<CreateOrUpdateBatchCommand> createOrUpdateBatchValidator,
    IStringLocalizer<ProductMessages> localizer,
    IBusinessClock businessClock)
    : IInventoryCommandService
{
    /// <summary>
    ///     Sums the quantity into the InventoryItem for (ProductId, WarehouseId)
    ///     when it already exists, or creates a new one otherwise — modeling
    ///     the real N:M relation (architecture doc §8.1): a product can now
    ///     have independent stock per warehouse, rather than the frontend's
    ///     original 1:1 model where choosing a different warehouse silently
    ///     moved the product. Always records a StockMovement.
    /// </summary>
    /// <summary>
    ///     Quantity == 0 is allowed (only negative is rejected): registering a
    ///     product with no initial stock still needs a real InventoryItem in
    ///     its chosen warehouse, or it stays invisible everywhere — not shown
    ///     as out-of-stock in that warehouse, and no StockLevelChangedEvent
    ///     ever fires for it, so it can never trigger an OUT_OF_STOCK alert
    ///     even when one clearly should exist. A StockMovement is only
    ///     recorded when Quantity &gt; 0 — "received 0 units" isn't a
    ///     meaningful audit entry.
    /// </summary>
    public async Task<Result<InventoryItem>> Handle(RegisterStockIntakeCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity < 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidQuantity, localizer[nameof(ProductError.InvalidQuantity)]);

        // Both reads go through the tenant-scoped repositories (AppDbContext's
        // BusinessId query filter), so a ProductId/WarehouseId belonging to
        // another business resolves to null here — without this check
        // nothing else in this method re-validates ownership, and a new
        // InventoryItem/StockMovement would otherwise get created tagged with
        // command.BusinessId but pointing at a product/warehouse it doesn't
        // actually own.
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result<InventoryItem>.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        var warehouse = await warehouseRepository.FindByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse == null)
            return Result<InventoryItem>.Failure(ProductError.WarehouseNotFound, localizer[nameof(ProductError.WarehouseNotFound)]);

        var existingItem = await inventoryItemRepository.FindByProductAndWarehouseAsync(command.ProductId,
            command.WarehouseId, cancellationToken);

        InventoryItem item;
        if (existingItem != null)
        {
            if (command.Quantity > 0) existingItem.AddStock(command.Quantity);
            if (command.MinimumStock.HasValue) existingItem.UpdateMinimumStock(command.MinimumStock.Value);
            inventoryItemRepository.Update(existingItem);
            item = existingItem;
        }
        else
        {
            // A brand-new warehouse for this product inherits the threshold
            // already configured on any of its sibling InventoryItems (kept in
            // sync by UpdateMinimumStockCommand) instead of silently starting
            // at 0 — otherwise splitting stock into a second warehouse would
            // reset "stock mínimo" for that portion until the product is
            // re-saved.
            var minimumStock = command.MinimumStock ?? (await inventoryItemRepository
                .FindAllByProductIdAsync(command.ProductId, cancellationToken))
                .Select(sibling => sibling.MinimumStock)
                .FirstOrDefault();

            item = new InventoryItem(command.ProductId, command.WarehouseId, command.BusinessId, command.Quantity,
                minimumStock);
            await inventoryItemRepository.AddAsync(item, cancellationToken);
        }

        if (command.Quantity > 0)
        {
            await stockMovementRepository.AddAsync(
                new StockMovement(command.ProductId, command.BusinessId, command.WarehouseId, command.Quantity,
                    StockMovementType.Intake, command.Supplier ?? string.Empty, command.Note ?? string.Empty,
                    command.SupplierId),
                cancellationToken);
        }

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone sold or restocked the same InventoryItem between this
            // request's read and its write. Before the concurrency token both
            // writes committed and the second overwrote the first's total,
            // quietly losing whichever movement lost the race.
            return Result<InventoryItem>.Failure(ProductError.ConcurrentModification,
                localizer[nameof(ProductError.ConcurrentModification)]);
        }

        // Batch data the caller supplied used to be accepted and then thrown
        // away: this handler never touched batchRepository, so registering
        // goods through the natural "stock intake" path left no batch, no
        // traceability, and no expiration alert could ever fire for them —
        // breaking the core "every product has an expiration date" rule.
        // Delegating keeps the upsert-in-place semantics and fires
        // BatchRegisteredEvent, so Alerts reacts exactly as it does when the
        // batch is registered directly.
        if (command.Expiration.HasValue || command.PurchasePrice.HasValue)
        {
            var batchResult = await Handle(
                new CreateOrUpdateBatchCommand(command.ProductId, command.BusinessId, command.Expiration,
                    command.PurchasePrice ?? 0m, item.Id),
                cancellationToken);

            if (batchResult.IsFailure)
                return Result<InventoryItem>.Failure(batchResult.Error!, batchResult.Message);
        }

        await PublishStockLevelChangedEvent(item, cancellationToken);

        return Result<InventoryItem>.Success(item);
    }

    /// <summary>
    ///     Decrements inventory after a confirmed sale. Not exposed as its own
    ///     REST endpoint — called via IProductContextFacade by Sales &amp; POS.
    ///
    ///     Takes no WarehouseId (matching Sales' contract, which sells a
    ///     product without picking a warehouse), so it spreads the deduction
    ///     across every InventoryItem the product has, oldest warehouse first
    ///     (WarehouseId ascending — a business's warehouses are consumed in
    ///     the order they were registered, e.g. "Almacén Principal" before a
    ///     later-added secondary one), spilling into the next warehouse only
    ///     once the current one is exhausted, until the full quantity is
    ///     accounted for. Sales already validates the SUM across warehouses
    ///     covers the sale (IProductContextFacade.GetAvailableStock) before
    ///     calling this; only decrementing a single item here would silently
    ///     under-deduct whenever no single warehouse alone holds the full
    ///     quantity. Records one StockMovement per warehouse actually touched.
    /// </summary>
    public async Task<Result<InventoryItem>> Handle(RegisterStockSaleCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity <= 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidQuantity, localizer[nameof(ProductError.InvalidQuantity)]);

        var items = (await inventoryItemRepository.FindAllByProductIdAsync(command.ProductId, cancellationToken))
            .Where(candidate => candidate.StockUnit > 0)
            .OrderBy(candidate => candidate.WarehouseId)
            .ToList();

        if (items.Sum(candidate => candidate.StockUnit) < command.Quantity)
            return Result<InventoryItem>.Failure(ProductError.InsufficientStock, localizer[nameof(ProductError.InsufficientStock)]);

        var remaining = command.Quantity;
        var touchedItems = new List<InventoryItem>();
        foreach (var item in items)
        {
            if (remaining <= 0) break;

            var deducted = Math.Min(remaining, item.StockUnit);
            item.RemoveStock(deducted);
            inventoryItemRepository.Update(item);
            touchedItems.Add(item);

            await stockMovementRepository.AddAsync(
                new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, deducted,
                    StockMovementType.Sale, string.Empty, string.Empty),
                cancellationToken);

            remaining -= deducted;
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        foreach (var item in touchedItems)
            await PublishStockLevelChangedEvent(item, cancellationToken);

        return Result<InventoryItem>.Success(touchedItems[0]);
    }

    /// <summary>
    ///     Puts units back after a sale is cancelled — the mirror of
    ///     RegisterStockSaleCommand, called by Sales through the facade.
    ///
    ///     Returns everything to the product's lowest-numbered warehouse,
    ///     which is the one the sale drew from first (that handler consumes
    ///     warehouses in WarehouseId order). For a single-warehouse bodega —
    ///     the real case here — that is exact. With stock split across
    ///     warehouses the totals stay right but the per-warehouse split can
    ///     shift, because a Sale does not record which warehouses it took
    ///     from; making that exact would mean storing the split on the sale
    ///     itself, which is not worth the schema for this shop.
    /// </summary>
    public async Task<Result<InventoryItem>> Handle(RegisterStockReturnCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity <= 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidQuantity, localizer[nameof(ProductError.InvalidQuantity)]);

        var item = (await inventoryItemRepository.FindAllByProductIdAsync(command.ProductId, cancellationToken))
            .OrderBy(candidate => candidate.WarehouseId)
            .FirstOrDefault();

        if (item == null)
            return Result<InventoryItem>.Failure(ProductError.InventoryItemNotFound,
                localizer[nameof(ProductError.InventoryItemNotFound)]);

        item.AddStock(command.Quantity);
        inventoryItemRepository.Update(item);

        await stockMovementRepository.AddAsync(
            new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, command.Quantity,
                StockMovementType.Return, string.Empty, "Venta cancelada"),
            cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);

        // Re-runs the alert rules: returning stock can take a product back
        // out of LOW_STOCK/OUT_OF_STOCK, which should resolve those alerts.
        await PublishStockLevelChangedEvent(item, cancellationToken);

        return Result<InventoryItem>.Success(item);
    }

    /// <summary>
    ///     Applies the same minimum-stock threshold to every InventoryItem the
    ///     product has (one per warehouse it's split into) — the product edit
    ///     form only exposes a single "stock mínimo" field, so this keeps that
    ///     one value in sync across all of a product's warehouses instead of
    ///     silently updating only the first one found. Returns the first item
    ///     (the caller only needs one to build the response resource).
    /// </summary>
    public async Task<Result<InventoryItem>> Handle(UpdateMinimumStockCommand command, CancellationToken cancellationToken)
    {
        // A negative threshold is meaningless and makes IsLowStock unreachable,
        // silently disabling the low-stock alert for that product.
        if (command.MinimumStock < 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidQuantity,
                localizer[nameof(ProductError.InvalidQuantity)]);

        var items = (await inventoryItemRepository.FindAllByProductIdAsync(command.ProductId, cancellationToken)).ToList();
        if (items.Count == 0)
            return Result<InventoryItem>.Failure(ProductError.InventoryItemNotFound,
                localizer[nameof(ProductError.InventoryItemNotFound)]);

        foreach (var item in items)
        {
            item.UpdateMinimumStock(command.MinimumStock);
            inventoryItemRepository.Update(item);
        }

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<InventoryItem>.Failure(ProductError.ConcurrentModification,
                localizer[nameof(ProductError.ConcurrentModification)]);
        }

        foreach (var item in items)
            await PublishStockLevelChangedEvent(item, cancellationToken);

        return Result<InventoryItem>.Success(items[0]);
    }

    /// <summary>
    ///     The product form only captures one expiration date per product (no
    ///     batch selector UI), so if the product already has an ACTIVE batch,
    ///     it's updated in place instead of piling up batches — otherwise
    ///     re-editing a product would keep creating new batches and the
    ///     "nearest expiration" calculation would keep surfacing the oldest
    ///     one instead of what the user just entered.
    /// </summary>
    public async Task<Result<Batch>> Handle(CreateOrUpdateBatchCommand command, CancellationToken cancellationToken)
    {
        if (!(await createOrUpdateBatchValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Batch>.Failure(ProductError.InvalidPurchasePrice, localizer[nameof(ProductError.InvalidPurchasePrice)]);

        // Tenant-scoped read (AppDbContext's BusinessId query filter), so a
        // ProductId belonging to another business resolves to null here —
        // without this check, a batch tagged with command.BusinessId could
        // otherwise get created pointing at a product it doesn't own.
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result<Batch>.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        if (command.Expiration.HasValue && command.Expiration.Value < businessClock.Today)
            return Result<Batch>.Failure(ProductError.InvalidExpirationDate,
                localizer[nameof(ProductError.InvalidExpirationDate)]);

        var existingBatch = await batchRepository.FindActiveByProductIdAsync(command.ProductId, cancellationToken);

        Batch batch;
        if (existingBatch != null)
        {
            existingBatch.UpdateDetails(command.Expiration, command.PurchasePrice, command.InventoryId);
            batchRepository.Update(existingBatch);
            batch = existingBatch;
        }
        else
        {
            batch = new Batch(command.ProductId, command.BusinessId, command.Expiration, command.PurchasePrice,
                command.InventoryId);
            await batchRepository.AddAsync(batch, cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        await mediator.PublishAsync(
            new BatchRegisteredEvent(batch.Id, batch.ProductId, product.Name, batch.BusinessId, batch.Expiration),
            cancellationToken);

        return Result<Batch>.Success(batch);
    }

    /// <summary>
    ///     Retires a batch whose goods left the shelf. This is what finally
    ///     lets an expired batch stop alerting: it stayed ACTIVE forever, so
    ///     the sweep re-raised the same "venció" alert hours after every time
    ///     it was resolved, and the shop had no way out. Publishing the event
    ///     also closes the alerts it left behind, so this is a single action.
    /// </summary>
    public async Task<Result<Batch>> Handle(DiscardBatchCommand command, CancellationToken cancellationToken)
    {
        var batch = await batchRepository.FindByIdAsync(command.BatchId, cancellationToken);
        if (batch == null)
            return Result<Batch>.Failure(ProductError.BatchNotFound, localizer[nameof(ProductError.BatchNotFound)]);

        if (batch.Status == BatchStatus.Inactive)
            return Result<Batch>.Failure(ProductError.BatchAlreadyDiscarded,
                localizer[nameof(ProductError.BatchAlreadyDiscarded)]);

        batch.Discard();
        batchRepository.Update(batch);
        await unitOfWork.CompleteAsync(cancellationToken);

        await mediator.PublishAsync(new BatchDiscardedEvent(batch.Id, batch.ProductId, batch.BusinessId), cancellationToken);

        return Result<Batch>.Success(batch);
    }

    private async Task PublishStockLevelChangedEvent(InventoryItem item, CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(item.ProductId, cancellationToken);
        await mediator.PublishAsync(
            new StockLevelChangedEvent(item.ProductId, product?.Name ?? string.Empty, item.WarehouseId, item.BusinessId,
                item.StockUnit, item.MinimumStock),
            cancellationToken);
    }
}
