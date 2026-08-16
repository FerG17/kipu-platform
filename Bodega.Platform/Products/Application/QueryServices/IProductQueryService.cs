using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Queries;

namespace Bodega.Platform.Products.Application.QueryServices;

public interface IProductQueryService
{
    Task<IEnumerable<Product>> Handle(GetAllProductsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<int>> Handle(GetProductSupplierIdsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> Handle(GetProductSupplierIdsByBusinessIdQuery query, CancellationToken cancellationToken);
}
