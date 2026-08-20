using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IProductQueryService
{
    Task<IEnumerable<Product>> Handle(GetAllProductsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<PagedResult<Product>> Handle(GetProductsPageByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<int>> Handle(GetProductSupplierIdsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> Handle(GetProductSupplierIdsByBusinessIdQuery query, CancellationToken cancellationToken);
}
