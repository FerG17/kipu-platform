using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PaymentPlanRepository(AppDbContext context) : BaseRepository<PaymentPlan>(context), IPaymentPlanRepository
{
    public async Task<PaymentPlan?> FindBySaleIdAsync(int saleId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>().FirstOrDefaultAsync(plan => plan.SaleId == saleId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> FindPendingByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>()
            .Where(plan => plan.BusinessId == businessId && !plan.IsCancelled
                                                           && plan.PaidInstallments < plan.TotalInstallments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> FindPendingByCustomerIdAsync(int customerId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from plan in Context.Set<PaymentPlan>()
            join sale in Context.Set<Sale>() on plan.SaleId equals sale.Id
            where sale.CustomerId == customerId && !plan.IsCancelled
                                                 && plan.PaidInstallments < plan.TotalInstallments
            select plan;

        return await query.ToListAsync(cancellationToken);
    }
}
