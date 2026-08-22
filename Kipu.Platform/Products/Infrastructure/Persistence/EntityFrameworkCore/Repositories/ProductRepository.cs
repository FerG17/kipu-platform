using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ProductRepository(AppDbContext context) : BaseRepository<Product>(context), IProductRepository
{
    /// <summary>
    ///     Excludes deactivated products by default: DELETE is a soft delete
    ///     (see ProductCommandService), so a "deleted" product must stop
    ///     showing up in the catalog even though the row stays for historical
    ///     sales and reports to resolve against. Fetching one by id still
    ///     returns it. Pass includeInactive to also surface deactivated
    ///     products — used by the "reactivate" screen.
    /// </summary>
    public async Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category, bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Product>().Where(product => product.BusinessId == businessId);
        if (!includeInactive) query = query.Where(product => product.Status == ProductStatus.Active);
        if (!string.IsNullOrEmpty(category)) query = query.Where(product => product.Category == category);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Product>> FindPageByBusinessIdAsync(int businessId, string? category, bool includeInactive,
        PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Product>().Where(product => product.BusinessId == businessId);
        if (!includeInactive) query = query.Where(product => product.Status == ProductStatus.Active);
        if (!string.IsNullOrEmpty(category)) query = query.Where(product => product.Category == category);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(product => product.Id).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<Product>(items, totalCount, page.Page, page.PageSize);
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
