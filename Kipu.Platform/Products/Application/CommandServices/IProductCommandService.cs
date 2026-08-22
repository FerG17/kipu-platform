using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Products.Application.CommandServices;

public interface IProductCommandService
{
    Task<Result<Product>> Handle(CreateProductCommand command, CancellationToken cancellationToken);
    Task<Result<Product>> Handle(UpdateProductCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ActivateProductCommand command, CancellationToken cancellationToken);
}
