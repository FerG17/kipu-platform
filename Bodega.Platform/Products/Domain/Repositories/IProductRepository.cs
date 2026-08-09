using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category,
        CancellationToken cancellationToken = default);
}
