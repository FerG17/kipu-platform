using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    /// <summary>
    ///     Excludes deactivated products: DELETE is a soft delete (see
    ///     ProductCommandService), so a "deleted" product must stop showing up
    ///     in the catalog even though the row stays for historical sales and
    ///     reports to resolve against. Fetching one by id still returns it.
    /// </summary>
    public async Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Product>()
            .Where(product => product.BusinessId == businessId && product.Status == ProductStatus.Active);
        if (!string.IsNullOrEmpty(category)) query = query.Where(product => product.Category == category);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> ListIgnoringTenantAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Product>().IgnoreQueryFilters().ToListAsync(cancellationToken);
    }

    public async Task<Product?> FindByBarcodeAsync(int businessId, string barcode, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Product>().FirstOrDefaultAsync(
            product => product.BusinessId == businessId && product.Status == ProductStatus.Active && product.Barcode == barcode,
            cancellationToken);
    }
}
