using Cortex.Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
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
    IValidator<RegisterStockIntakeCommand> registerStockIntakeValidator,
    IValidator<AdjustStockCommand> adjustStockValidator,
    IStringLocalizer<ProductMessages> localizer,
    IBusinessClock businessClock,
    ILogger<InventoryCommandService> logger)
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

        // X4 M9: Quantity's upper bound and Supplier/Note's lengths had no
        // validation at all before this — an absurd quantity (typo, runaway
        // script) could accumulate toward InventoryItem.StockUnit overflowing
        // (X4 A6), and an oversized Supplier/Note would only be caught by
        // MySQL, as an unhandled 500 (see the DbUpdateException catch below,
        // X4 M9's other half).
        var intakeValidation = await registerStockIntakeValidator.ValidateAsync(command, cancellationToken);
        if (!intakeValidation.IsValid)
            return Result<InventoryItem>.Failure(ProductError.InvalidStockIntakeData,
                localizer[nameof(ProductError.InvalidStockIntakeData)]);

        // Validated here too (not just inside the delegated CreateOrUpdateBatch
        // call below) so an invalid expiration/price is rejected before this
        // method touches the InventoryItem/StockMovement — the two now share
        // one transaction (X4 A9), but failing fast here still avoids doing
        // any of that work only to roll it back.
        if (command.Expiration.HasValue && command.Expiration.Value < businessClock.Today)
            return Result<InventoryItem>.Failure(ProductError.InvalidExpirationDate,
                localizer[nameof(ProductError.InvalidExpirationDate)]);

        if (command.PurchasePrice.HasValue && command.PurchasePrice.Value < 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidPurchasePrice,
                localizer[nameof(ProductError.InvalidPurchasePrice)]);

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

        if (!product.IsActive)
            return Result<InventoryItem>.Failure(ProductError.ProductInactive, localizer[nameof(ProductError.ProductInactive)]);

        var warehouse = await warehouseRepository.FindByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse == null)
            return Result<InventoryItem>.Failure(ProductError.WarehouseNotFound, localizer[nameof(ProductError.WarehouseNotFound)]);

        // X4 M10: nothing checked this before — a deactivated warehouse kept
        // silently accepting new stock forever.
        if (warehouse.Status != WarehouseStatus.Active)
            return Result<InventoryItem>.Failure(ProductError.WarehouseInactive, localizer[nameof(ProductError.WarehouseInactive)]);

        var existingItem = await inventoryItemRepository.FindByProductAndWarehouseAsync(command.ProductId,
            command.WarehouseId, cancellationToken);

        // X4 A9: the InventoryItem/StockMovement write and the batch upsert
        // below used to be two independent SaveChanges calls — if the batch
        // one failed (e.g. a race on Expiration), stock was already increased
        // with no batch and no expiration tracking for it, and no way to tell
        // from the error alone that part of the request had already landed.
        // A single SaveChangesAsync call is already atomic on its own (EF
        // Core wraps it in an implicit transaction) — that's the actual fix;
        // there is deliberately no explicit BeginTransactionAsync here, since
        // this method is itself called from inside PurchaseOrderCommandService's
        // own explicit transaction (receiving an order), and EF Core does not
        // support a nested one.
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
            // already configured on any of its sibling InventoryItems (kept
            // in sync by UpdateMinimumStockCommand) instead of silently
            // starting at 0 — otherwise splitting stock into a second
            // warehouse would reset "stock mínimo" for that portion until
            // the product is re-saved.
            var minimumStock = command.MinimumStock ?? (await inventoryItemRepository
                .FindAllByProductIdAsync(command.ProductId, cancellationToken))
                .Select(sibling => sibling.MinimumStock)
                .FirstOrDefault();

            item = new InventoryItem(command.ProductId, command.WarehouseId, command.BusinessId, command.Quantity,
                minimumStock);
            await inventoryItemRepository.AddAsync(item, cancellationToken);
        }

        // X5 Bloque C: every intake that carries an expiration/cost opens a
        // NEW lot instead of overwriting whatever batch the product already
        // had — a product can now have several active lots at once (e.g. an
        // early restock that expires later than stock already on the
        // shelf). Linked via the InventoryItem navigation, not `item.Id`
        // copied by hand — item may not be persisted yet when item is a
        // brand-new InventoryItem, and EF Core's key fixup only resolves the
        // real id through a tracked navigation (X5 #9's InventoryId=0 bug).
        // Queued on the tracker only (no commit yet) so it lands in the SAME
        // SaveChanges call as the InventoryItem/StockMovement below.
        Batch? newBatch = null;
        if (command.Quantity > 0 && (command.Expiration.HasValue || command.PurchasePrice.HasValue))
        {
            newBatch = new Batch(command.ProductId, command.BusinessId, command.Expiration, command.PurchasePrice ?? 0m,
                command.Quantity).LinkToInventoryItem(item);
            await batchRepository.AddAsync(newBatch, cancellationToken);
        }

        if (command.Quantity > 0)
        {
            await stockMovementRepository.AddAsync(
                new StockMovement(command.ProductId, command.BusinessId, command.WarehouseId, command.Quantity,
                    StockMovementType.Intake, command.Supplier ?? string.Empty, command.Note ?? string.Empty,
                    command.SupplierId, newBatch),
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
        catch (OverflowException)
        {
            // X4 A6: InventoryItem.AddStock/AdjustStock now use `checked`
            // arithmetic specifically so this can be caught here instead of
            // silently wrapping StockUnit to a negative value that reads as
            // neither low nor out of stock. Thrown synchronously by AddStock
            // above, before SaveChanges is ever called, so nothing here was
            // persisted — there is nothing to roll back.
            logger.LogError(
                "Stock intake overflowed StockUnit for product {ProductId} in business {BusinessId} (quantity {Quantity})",
                command.ProductId, command.BusinessId, command.Quantity);
            return Result<InventoryItem>.Failure(ProductError.InvalidQuantity, localizer[nameof(ProductError.InvalidQuantity)]);
        }

        if (newBatch != null)
            await PublishBatchRegisteredEventSafely(newBatch, product.Name, cancellationToken);

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
    ///     quantity.
    ///
    ///     X5 Bloque C: within each InventoryItem touched, also draws down
    ///     its active batches earliest-expiration-first (FEFO), recording one
    ///     StockMovement per lot actually touched — plus one more, with no
    ///     BatchId, for any portion not covered by a tracked lot (stock that
    ///     predates per-lot tracking). A product with no batches at all keeps
    ///     behaving exactly as before this block existed.
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
            remaining -= deducted;

            var batchRemaining = deducted;
            var activeBatches = await batchRepository.FindActiveByInventoryItemIdAsync(item.Id, cancellationToken);
            foreach (var batch in activeBatches)
            {
                if (batchRemaining <= 0) break;
                if (batch.RemainingQuantity <= 0) continue;

                var deductedFromBatch = Math.Min(batchRemaining, batch.RemainingQuantity);
                batch.Reduce(deductedFromBatch);
                batchRepository.Update(batch);
                batchRemaining -= deductedFromBatch;

                await stockMovementRepository.AddAsync(
                    new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, deductedFromBatch,
                        StockMovementType.Sale, string.Empty, string.Empty, batch: batch),
                    cancellationToken);
            }

            if (batchRemaining > 0)
            {
                await stockMovementRepository.AddAsync(
                    new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, batchRemaining,
                        StockMovementType.Sale, string.Empty, string.Empty),
                    cancellationToken);
            }
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
    ///
    ///     X5 Bloque C: restores into the batch closest to expiring first,
    ///     same approximation as the warehouse split above and for the same
    ///     reason — RestoreStock only carries ProductId + Quantity, not which
    ///     lot the original sale line actually drew from (Sales doesn't
    ///     track that), so this can't trace back to the exact batch. A batch
    ///     already at full Quantity, or discarded, is skipped.
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

        var remaining = command.Quantity;
        var activeBatches = await batchRepository.FindActiveByInventoryItemIdAsync(item.Id, cancellationToken);
        foreach (var batch in activeBatches)
        {
            if (remaining <= 0) break;

            var capacity = batch.Quantity - batch.RemainingQuantity;
            if (capacity <= 0) continue;

            var restored = Math.Min(remaining, capacity);
            batch.Restore(restored);
            batchRepository.Update(batch);
            remaining -= restored;

            await stockMovementRepository.AddAsync(
                new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, restored,
                    StockMovementType.Return, string.Empty, "Venta cancelada", batch: batch),
                cancellationToken);
        }

        if (remaining > 0)
        {
            await stockMovementRepository.AddAsync(
                new StockMovement(command.ProductId, command.BusinessId, item.WarehouseId, remaining,
                    StockMovementType.Return, string.Empty, "Venta cancelada"),
                cancellationToken);
        }

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
    ///     Manual stock correction not tied to a sale — shrinkage, breakage,
    ///     theft, or fixing a physical count (I25). Delta is signed: negative
    ///     removes units, positive adds them. Records a StockMovement with the
    ///     signed delta as its Quantity (unlike every other movement type,
    ///     which is always positive and relies on Type alone for direction),
    ///     and re-evaluates LOW_STOCK/OUT_OF_STOCK the same way every other
    ///     stock mutation does.
    /// </summary>
    public async Task<Result<InventoryItem>> Handle(AdjustStockCommand command, CancellationToken cancellationToken)
    {
        if (command.Delta == 0)
            return Result<InventoryItem>.Failure(ProductError.InvalidAdjustmentQuantity,
                localizer[nameof(ProductError.InvalidAdjustmentQuantity)]);

        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result<InventoryItem>.Failure(ProductError.AdjustmentReasonRequired,
                localizer[nameof(ProductError.AdjustmentReasonRequired)]);

        // X4 M9: bounds command.Delta's magnitude and command.Reason's length —
        // neither had any validation before.
        var adjustmentValidation = await adjustStockValidator.ValidateAsync(command, cancellationToken);
        if (!adjustmentValidation.IsValid)
            return Result<InventoryItem>.Failure(ProductError.InvalidAdjustmentReason,
                localizer[nameof(ProductError.InvalidAdjustmentReason)]);

        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result<InventoryItem>.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        if (!product.IsActive)
            return Result<InventoryItem>.Failure(ProductError.ProductInactive, localizer[nameof(ProductError.ProductInactive)]);

        // X4 M10: an adjustment against a deactivated warehouse used to go
        // through with no check at all — nothing here ever looked the
        // warehouse itself up before.
        var warehouse = await warehouseRepository.FindByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse == null)
            return Result<InventoryItem>.Failure(ProductError.WarehouseNotFound, localizer[nameof(ProductError.WarehouseNotFound)]);

        if (warehouse.Status != WarehouseStatus.Active)
            return Result<InventoryItem>.Failure(ProductError.WarehouseInactive, localizer[nameof(ProductError.WarehouseInactive)]);

        var item = await inventoryItemRepository.FindByProductAndWarehouseAsync(command.ProductId, command.WarehouseId,
            cancellationToken);
        if (item == null)
            return Result<InventoryItem>.Failure(ProductError.InventoryItemNotFound,
                localizer[nameof(ProductError.InventoryItemNotFound)]);

        if (item.StockUnit + command.Delta < 0)
            return Result<InventoryItem>.Failure(ProductError.AdjustmentExceedsAvailableStock,
                localizer[nameof(ProductError.AdjustmentExceedsAvailableStock)]);

        await stockMovementRepository.AddAsync(
            new StockMovement(command.ProductId, command.BusinessId, command.WarehouseId, command.Delta,
                StockMovementType.Adjustment, string.Empty, command.Reason),
            cancellationToken);

        try
        {
            item.AdjustStock(command.Delta);
            inventoryItemRepository.Update(item);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<InventoryItem>.Failure(ProductError.ConcurrentModification,
                localizer[nameof(ProductError.ConcurrentModification)]);
        }
        catch (OverflowException)
        {
            // X4 A6 — see the matching catch in Handle(RegisterStockIntakeCommand).
            logger.LogError(
                "Stock adjustment overflowed StockUnit for product {ProductId} in business {BusinessId} (delta {Delta})",
                command.ProductId, command.BusinessId, command.Delta);
            return Result<InventoryItem>.Failure(ProductError.InvalidAdjustmentReason,
                localizer[nameof(ProductError.InvalidAdjustmentReason)]);
        }

        await PublishStockLevelChangedEvent(item, cancellationToken);

        return Result<InventoryItem>.Success(item);
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

    /// <summary>
    ///     Always called after the InventoryItem change it reports on has
    ///     already committed — so a failure here (e.g. an alert handler
    ///     throwing) is caught and logged rather than propagated, instead of
    ///     turning an already-successful stock change into an HTTP failure
    ///     the caller reasonably reads as "nothing was saved" (X5 #8).
    /// </summary>
    private async Task PublishStockLevelChangedEvent(InventoryItem item, CancellationToken cancellationToken)
    {
        try
        {
            var product = await productRepository.FindByIdAsync(item.ProductId, cancellationToken);
            await mediator.PublishAsync(
                new StockLevelChangedEvent(item.ProductId, product?.Name ?? string.Empty, item.WarehouseId, item.BusinessId,
                    item.StockUnit, item.MinimumStock),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "StockLevelChangedEvent handling failed for product {ProductId} in warehouse {WarehouseId}, business {BusinessId} — the stock change itself already committed",
                item.ProductId, item.WarehouseId, item.BusinessId);
        }
    }

    /// <summary>
    ///     Always called after the batch change it reports on has already
    ///     committed — same rationale as PublishStockLevelChangedEvent above
    ///     (X5 #8).
    /// </summary>
    private async Task PublishBatchRegisteredEventSafely(Batch batch, string productName, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.PublishAsync(
                new BatchRegisteredEvent(batch.Id, batch.ProductId, productName, batch.BusinessId, batch.Expiration),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "BatchRegisteredEvent handling failed for batch {BatchId} (product {ProductId}, business {BusinessId}) — the batch itself already committed",
                batch.Id, batch.ProductId, batch.BusinessId);
        }
    }
}
