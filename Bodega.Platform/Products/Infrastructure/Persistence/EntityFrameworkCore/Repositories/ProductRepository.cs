using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    public async Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Product>().Where(product => product.BusinessId == businessId);
        if (!string.IsNullOrEmpty(category)) query = query.Where(product => product.Category == category);
        return await query.ToListAsync(cancellationToken);
    }
}
