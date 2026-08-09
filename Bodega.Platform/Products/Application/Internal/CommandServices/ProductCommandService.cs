using Cortex.Mediator;
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
    IStringLocalizer<ProductMessages> localizer)
    : IProductCommandService
{
    public async Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Product(command.BusinessId, command.Name, command.Description, command.Category,
            command.BasePrice);
        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        await mediator.PublishAsync(new ProductCreatedEvent(product.Id, product.BusinessId, product.Name),
            cancellationToken);

        return Result<Product>.Success(product);
    }

    public async Task<Result<Product>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result<Product>.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        product.UpdateDetails(command.Name, command.Description, command.Category, command.BasePrice);
        productRepository.Update(product);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Product>.Success(product);
    }

    /// <summary>Business rule: deletion is blocked while any InventoryItem for this product still has stock.</summary>
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await productRepository.FindByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
            return Result.Failure(ProductError.ProductNotFound, localizer[nameof(ProductError.ProductNotFound)]);

        var inventoryItems = await inventoryItemRepository.FindAllByProductIdAsync(command.ProductId, cancellationToken);
        if (inventoryItems.Any(item => item.StockUnit > 0))
            return Result.Failure(ProductError.CannotDeleteWithStock,
                localizer[nameof(ProductError.CannotDeleteWithStock)]);

        productRepository.Remove(product);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }
}
