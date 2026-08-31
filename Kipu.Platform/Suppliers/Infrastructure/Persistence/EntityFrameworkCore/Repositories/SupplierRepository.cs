using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SupplierRepository(AppDbContext context) : BaseRepository<Supplier>(context), ISupplierRepository
{
    public async Task<PagedResult<Supplier>> FindAllByBusinessIdAsync(int businessId, PageRequest page, bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Supplier>().Where(supplier => supplier.BusinessId == businessId);
        if (!includeInactive) query = query.Where(supplier => supplier.Status == SupplierStatus.Active);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(supplier => supplier.Id).Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<Supplier>(items, totalCount, page.Page, page.PageSize);
    }

    public async Task<IReadOnlyCollection<int>> FindExistingIdsAsync(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Supplier>()
            .Where(supplier => supplier.BusinessId == businessId && supplierIds.Contains(supplier.Id))
            .Select(supplier => supplier.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — see ISupplierRepository.</summary>
    public async Task<IEnumerable<Supplier>> FindAllIgnoringTenantByIdsAsync(IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];

        return await Context.Set<Supplier>().IgnoreQueryFilters()
            .Where(supplier => idList.Contains(supplier.Id)).ToListAsync(cancellationToken);
    }
}
