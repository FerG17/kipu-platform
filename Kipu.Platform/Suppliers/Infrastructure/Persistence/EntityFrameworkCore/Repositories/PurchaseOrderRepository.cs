using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PurchaseOrderRepository(AppDbContext context) : BaseRepository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    public async Task<PagedResult<PurchaseOrder>> FindAllByBusinessIdAsync(int businessId, PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<PurchaseOrder>().Where(order => order.BusinessId == businessId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Include(order => order.Details).OrderByDescending(order => order.Date)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<PurchaseOrder>(items, totalCount, page.Page, page.PageSize);
    }

    public async Task<IEnumerable<PurchaseOrder>> FindAllBySupplierIdAsync(int supplierId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PurchaseOrder>().Include(order => order.Details)
            .Where(order => order.SupplierId == supplierId).ToListAsync(cancellationToken);
    }

    public async Task<PurchaseOrder?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PurchaseOrder>().Include(order => order.Details)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task<(PurchaseOrder Order, PurchaseOrderDetail Detail)?> FindByDetailIdAsync(int detailId,
        CancellationToken cancellationToken = default)
    {
        var detail = await Context.Set<PurchaseOrderDetail>().FindAsync([detailId], cancellationToken);
        if (detail == null) return null;

        var order = await FindByIdWithDetailsAsync(detail.PurchaseId, cancellationToken);
        return order == null ? null : (order, detail);
    }

    /// <summary>IgnoreQueryFilters() deliberately — see IPurchaseOrderRepository.</summary>
    public async Task<IEnumerable<PurchaseOrder>> FindAllIgnoringTenantByIdsAsync(IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];

        return await Context.Set<PurchaseOrder>().IgnoreQueryFilters()
            .Where(order => idList.Contains(order.Id)).ToListAsync(cancellationToken);
    }
}
