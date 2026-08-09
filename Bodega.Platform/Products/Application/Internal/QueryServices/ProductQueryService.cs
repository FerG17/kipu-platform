using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.QueryServices;

public class ProductQueryService(IProductRepository productRepository) : IProductQueryService
{
    public async Task<IEnumerable<Product>> Handle(GetAllProductsByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindAllByBusinessIdAsync(query.BusinessId, query.Category, cancellationToken);
    }

    public async Task<Product?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        return await productRepository.FindByIdAsync(query.ProductId, cancellationToken);
    }
}
