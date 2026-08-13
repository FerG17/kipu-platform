using Cortex.Mediator;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Products.Application.CommandServices;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Domain.Model.Errors;
using Bodega.Platform.Products.Domain.Model.Events;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Products.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.CommandServices;

public class ProductCommandService(
    IProductRepository productRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IValidator<CreateProductCommand> createProductValidator,
    IValidator<UpdateProductCommand> updateProductValidator,
    IStringLocalizer<ProductMessages> localizer)
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

        var product = new Product(command.BusinessId, command.Name, command.Description, command.Category,
            command.BasePrice, command.Barcode);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

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

        product.UpdateDetails(command.Name, command.Description, command.Category, command.BasePrice, command.Barcode);
        productRepository.Update(product);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Product>.Success(product);
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
}
