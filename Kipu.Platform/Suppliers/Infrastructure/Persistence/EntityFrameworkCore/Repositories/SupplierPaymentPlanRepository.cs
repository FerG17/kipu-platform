using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SupplierPaymentPlanRepository(AppDbContext context)
    : BaseRepository<SupplierPaymentPlan>(context), ISupplierPaymentPlanRepository
{
    public async Task<SupplierPaymentPlan?> FindByIdWithScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<SupplierPaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<SupplierPaymentPlan?> FindByPurchaseOrderIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<SupplierPaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .FirstOrDefaultAsync(plan => plan.PurchaseOrderId == purchaseOrderId, cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentPlan>> FindPendingByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<SupplierPaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .Where(plan => plan.BusinessId == businessId && !plan.IsCancelled
                                                           && plan.PaidInstallments < plan.TotalInstallments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentPlan>> FindPendingBySupplierIdAsync(int supplierId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from plan in Context.Set<SupplierPaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            join order in Context.Set<PurchaseOrder>() on plan.PurchaseOrderId equals order.Id
            where order.SupplierId == supplierId && !plan.IsCancelled
                                                  && plan.PaidInstallments < plan.TotalInstallments
            select plan;

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — see ISupplierPaymentPlanRepository.</summary>
    public async Task<IEnumerable<SupplierPaymentPlan>> FindAllPendingAcrossBusinessesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<SupplierPaymentPlan>().IgnoreQueryFilters().Include(plan => plan.Installments)
            .Where(plan => !plan.IsCancelled && plan.PaidInstallments < plan.TotalInstallments)
            .ToListAsync(cancellationToken);
    }
}
