using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Queries;

namespace Bodega.Platform.Products.Application.QueryServices;

public interface IProductQueryService
{
    Task<IEnumerable<Product>> Handle(GetAllProductsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken);
}
