using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

public class ProductQueryService(IProductRepository productRepository, IProductSupplierRepository productSupplierRepository)
    : IProductQueryService
{
    public async Task<IEnumerable<Product>> Handle(GetAllProductsByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindAllByBusinessIdAsync(query.BusinessId, query.Category, query.IncludeInactive, cancellationToken);
    }

    public async Task<PagedResult<Product>> Handle(GetProductsPageByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindPageByBusinessIdAsync(query.BusinessId, query.Category, query.IncludeInactive, query.Page,
            cancellationToken);
    }

    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> Handle(GetProductSupplierIdsQuery query, CancellationToken cancellationToken)
    {
        var links = await productSupplierRepository.FindByProductIdAsync(query.ProductId, cancellationToken);
        return links.Select(link => link.SupplierId).ToList();
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> Handle(GetProductSupplierIdsByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await productSupplierRepository.FindSupplierIdsGroupedByProductAsync(query.BusinessId, cancellationToken);
    }
}
