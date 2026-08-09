using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Products.Application.CommandServices;

public interface IProductCommandService
{
    Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken);
    Task<Result<Product>> Handle(UpdateProductCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken);
}
