using Cortex.Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Products.Application.Internal.CommandServices;

public class ProductCommandService(
    IProductRepository productRepository,
    IInventoryItemRepository inventoryItemRepository,
    IProductSupplierRepository productSupplierRepository,
    ISupplierContextFacade supplierContextFacade,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IValidator<CreateProductCommand> createProductValidator,
    IValidator<UpdateProductCommand> updateProductValidator,
    IStringLocalizer<ProductMessages> localizer,
    ILogger<ProductCommandService> logger)
    : IProductCommandService
{
    public async Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // Nothing checked these before: a blank name was accepted, a negative
        // price landed in the inventory-value KPI, and a name past its
        // varchar(150) reached MySQL and came back as a 500.
        if (!(await createProductValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Product>.Failure(ProductError.InvalidProductData,
                localizer[nameof(ProductError.InvalidProductData)]);

        if (!string.IsNullOrEmpty(command.Barcode) &&
            await productRepository.FindByBarcodeAsync(command.BusinessId, command.Barcode, cancellationToken) != null)
            return Result<Product>.Failure(ProductError.DuplicateBarcode,
                localizer[nameof(ProductError.DuplicateBarcode)]);

        var supplierIds = (command.SupplierIds ?? []).Distinct().ToList();
        if (!await AllSuppliersExist(command.BusinessId, supplierIds, cancellationToken))
            return Result<Product>.Failure(ProductError.SupplierNotFound, localizer[nameof(ProductError.SupplierNotFound)]);

        // X4 M12: product creation and its supplier links used to be two
        // independent commits — if the second one failed, the product was
        // already committed with no suppliers tagged, and nothing rolled it
        // back or reported that half the request never landed.
        var product = new Product(command.BusinessId, command.Name, command.Description, command.Category,
            command.BasePrice, command.Barcode, command.UnitOfSale, command.UnidadDeMedida, command.Presentacion);
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                await productRepository.AddAsync(product, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken); // product.Id is now populated.

                foreach (var supplierId in supplierIds)
                    await productSupplierRepository.AddAsync(new ProductSupplier(product.Id, supplierId, command.BusinessId),
                        cancellationToken);
                if (supplierIds.Count > 0) await unitOfWork.CompleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);

                // The read-then-write check above has a race window (two
                // concurrent creates with the same barcode, both pass the
                // check) — the unique index on (BusinessId, ActiveBarcode) is
                // the real guarantee. But X4 M12: this used to map EVERY
                // DbUpdateException here to "duplicate barcode", including an
                // unrelated FK violation or connection drop, misleading
                // whoever read the error. Re-checking whether the barcode
                // genuinely collides distinguishes the two without depending
                // on the database provider's specific exception shape.
                if (!string.IsNullOrEmpty(command.Barcode) &&
                    await productRepository.FindByBarcodeAsync(command.BusinessId, command.Barcode, cancellationToken) != null)
                    return Result<Product>.Failure(ProductError.DuplicateBarcode,
                        localizer[nameof(ProductError.DuplicateBarcode)]);

                logger.LogError(exception, "Failed to create product for business {BusinessId}", command.BusinessId);
                return Result<Product>.Failure(ProductError.DatabaseError, localizer[nameof(ProductError.DatabaseError)]);
            }
        }

        await mediator.PublishAsync(new ProductCreatedEvent(product.Id, product.BusinessId, product.Name),
            cancellationToken);

        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        if (!(await updateProductValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Product>.Failure(ProductError.InvalidProductData,
                localizer[nameof(ProductError.InvalidProductData)]);

        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result<Product>.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        if (!string.IsNullOrEmpty(command.Barcode))
        {
            var existing = await productRepository.FindByBarcodeAsync(product.BusinessId, command.Barcode, cancellationToken);
            if (existing != null && existing.Id != product.Id)
                return Result<Product>.Failure(ProductError.DuplicateBarcode,
                    localizer[nameof(ProductError.DuplicateBarcode)]);
        }

        var supplierIds = (command.SupplierIds ?? []).Distinct().ToList();
        if (!await AllSuppliersExist(product.BusinessId, supplierIds, cancellationToken))
            return Result<Product>.Failure(ProductError.SupplierNotFound, localizer[nameof(ProductError.SupplierNotFound)]);

        product.UpdateDetails(command.Name, command.Description, command.Category, command.BasePrice, command.Barcode,
            command.UnitOfSale, command.UnidadDeMedida, command.Presentacion);
        productRepository.Update(product);
        await SyncProductSuppliers(product.Id, product.BusinessId, supplierIds, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Product>.Success(product);
    }

    /// <summary>Empty is trivially valid — a product may legitimately have no supplier tagged yet.</summary>
    private async Task<bool> AllSuppliersExist(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken)
    {
        if (supplierIds.Count == 0) return true;
        var existing = await supplierContextFacade.FilterExistingSupplierIds(businessId, supplierIds, cancellationToken);
        return existing.Count == supplierIds.Count;
    }

    /// <summary>
    ///     Replaces a product's supplier tags with the given set — removes
    ///     links no longer wanted, adds new ones, leaves unchanged ones alone.
    ///     Queues changes on the change tracker only; the caller commits.
    /// </summary>
    private async Task SyncProductSuppliers(int productId, int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken)
    {
        var wanted = supplierIds.ToHashSet();
        var existingLinks = await productSupplierRepository.FindByProductIdAsync(productId, cancellationToken);
        var existingIds = existingLinks.Select(link => link.SupplierId).ToHashSet();

        foreach (var link in existingLinks.Where(link => !wanted.Contains(link.SupplierId)))
            productSupplierRepository.Remove(link);

        foreach (var supplierId in wanted.Where(id => !existingIds.Contains(id)))
            await productSupplierRepository.AddAsync(new ProductSupplier(productId, supplierId, businessId), cancellationToken);
    }

    /// <summary>
    ///     Deactivates the product rather than physically deleting it.
    ///
    ///     A hard delete could never actually succeed: alerts, sale lines and
    ///     purchase lines all hold RESTRICT foreign keys to products, so the
    ///     database refused the delete and the request died as an unhandled
    ///     DbUpdateException (a 500 to the caller). The only product that got
    ///     past the "still has stock" guard below was one sitting at zero —
    ///     which is precisely the one the alert engine has just flagged
    ///     OUT_OF_STOCK, guaranteeing the crash.
    ///
    ///     Soft delete is also the right domain answer regardless: a product
    ///     that has ever been sold has to stay resolvable, or historical sales
    ///     and reports lose the item they refer to. Product already modelled
    ///     this (ProductStatus.Inactive, Deactivate(), IsActive) — it was
    ///     simply never wired up.
    /// </summary>
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        var inventoryItems = await inventoryItemRepository.FindAllByProductIdAsync(command.ProductId, cancellationToken);
        if (inventoryItems.Any(item => item.StockUnit > 0))
            return Result.Failure(ProductError.CannotDeleteWithStock,
                localizer[nameof(ProductError.CannotDeleteWithStock)]);

        product.Deactivate();
        productRepository.Update(product);
        await unitOfWork.CompleteAsync(cancellationToken);

        // Lets Alerts close anything still open for it, so an alert never
        // outlives the product it points at.
        await mediator.PublishAsync(new ProductDeactivatedEvent(product.Id, product.BusinessId), cancellationToken);

        return Result.Success();
    }

    /// <summary>
    ///     Undoes DeleteProductCommand's soft delete — brings a discontinued
    ///     product back into the active catalog.
    ///
    ///     X4 A8: while inactive, another product could have taken this one's
    ///     barcode (the unique index only constrains ACTIVE products — see
    ///     the migration doc comment). Reactivating without re-checking used
    ///     to hit that index, surface as a raw 500, and leave the product
    ///     permanently stuck INACTIVE with no way to tell the owner why.
    /// </summary>
    public async Task<Result> Handle(ActivateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        if (!string.IsNullOrEmpty(product.Barcode))
        {
            var barcodeOwner = await productRepository.FindByBarcodeAsync(product.BusinessId, product.Barcode, cancellationToken);
            if (barcodeOwner != null && barcodeOwner.Id != product.Id)
                return Result.Failure(ProductError.DuplicateBarcode, localizer[nameof(ProductError.DuplicateBarcode)]);
        }

        product.Activate();
        productRepository.Update(product);
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // Same race-window reasoning as Handle(CreateProductCommand): the
            // check above has a gap two concurrent reactivations could both
            // pass, and the unique index is the real guarantee.
            var barcodeOwner = !string.IsNullOrEmpty(product.Barcode)
                ? await productRepository.FindByBarcodeAsync(product.BusinessId, product.Barcode, cancellationToken)
                : null;
            if (barcodeOwner != null && barcodeOwner.Id != product.Id)
                return Result.Failure(ProductError.DuplicateBarcode, localizer[nameof(ProductError.DuplicateBarcode)]);

            logger.LogError(exception, "Failed to activate product {ProductId} for business {BusinessId}",
                product.Id, product.BusinessId);
            return Result.Failure(ProductError.DatabaseError, localizer[nameof(ProductError.DatabaseError)]);
        }

        return Result.Success();
    }
}
