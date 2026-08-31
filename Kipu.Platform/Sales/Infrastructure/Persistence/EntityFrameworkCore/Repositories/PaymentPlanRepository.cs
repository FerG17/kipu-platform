using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PaymentPlanRepository(AppDbContext context, IBusinessClock businessClock)
    : BaseRepository<PaymentPlan>(context), IPaymentPlanRepository
{
    /// <summary>
    ///     The base FindByIdAsync (IBaseRepository, shared by every entity
    ///     type) never eager-loads Payments — same reasoning as
    ///     ISaleRepository.FindByIdWithDetailsAsync next to Sale's own base
    ///     FindByIdAsync. Command handlers that read or mutate Payments
    ///     (register/revert a payment) must use this one instead, or
    ///     RevertLastPayment finds an empty collection and PaymentPlanResource
    ///     silently reports no history even when one exists.
    /// </summary>
    public async Task<PaymentPlan?> FindByIdWithPaymentsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
    }

    public async Task<PaymentPlan?> FindBySaleIdAsync(int saleId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .FirstOrDefaultAsync(plan => plan.SaleId == saleId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> FindPendingByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            .Where(plan => plan.BusinessId == businessId && !plan.IsCancelled
                                                           && plan.PaidInstallments < plan.TotalInstallments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> FindPendingByCustomerIdAsync(int customerId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from plan in Context.Set<PaymentPlan>().Include(plan => plan.Payments).Include(plan => plan.Installments)
            join sale in Context.Set<Sale>() on plan.SaleId equals sale.Id
            where sale.CustomerId == customerId && !plan.IsCancelled
                                                 && plan.PaidInstallments < plan.TotalInstallments
            select plan;

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — see IPaymentPlanRepository.</summary>
    public async Task<IEnumerable<PaymentPlan>> FindAllPendingAcrossBusinessesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentPlan>().IgnoreQueryFilters().Include(plan => plan.Installments)
            .Where(plan => !plan.IsCancelled && plan.PaidInstallments < plan.TotalInstallments)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InstallmentPayment>> FindCollectedInstallmentPaymentsByBusinessIdAsync(int businessId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<PaymentPlan>()
            .Where(plan => plan.BusinessId == businessId)
            .SelectMany(plan => plan.Payments.Where(payment => !payment.IsReversed));

        if (dateFrom.HasValue) query = query.Where(payment => payment.PaidAt >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(payment => payment.PaidAt <= businessClock.EndOfDay(dateTo.Value));

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> FindBySaleIdsAsync(IEnumerable<int> saleIds,
        CancellationToken cancellationToken = default)
    {
        var saleIdList = saleIds.ToList();
        return await Context.Set<PaymentPlan>().Include(plan => plan.Payments)
            .Where(plan => saleIdList.Contains(plan.SaleId)).ToListAsync(cancellationToken);
    }
}
