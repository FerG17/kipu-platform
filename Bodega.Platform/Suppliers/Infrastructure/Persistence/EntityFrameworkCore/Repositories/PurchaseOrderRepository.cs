using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Entities;
using Bodega.Platform.Suppliers.Domain.Repositories;

namespace Bodega.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PurchaseOrderRepository(AppDbContext context) : BaseRepository<PurchaseOrder>(context), IPurchaseOrderRepository
{
    public async Task<IEnumerable<PurchaseOrder>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PurchaseOrder>().Include(order => order.Details)
            .Where(order => order.BusinessId == businessId).ToListAsync(cancellationToken);
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
}
