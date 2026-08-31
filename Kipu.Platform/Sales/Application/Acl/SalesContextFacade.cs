using Kipu.Platform.Shared.Application;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Sales.Interfaces.Acl;

namespace Kipu.Platform.Sales.Application.Acl;

public class SalesContextFacade(ISaleRepository saleRepository, IPaymentPlanRepository paymentPlanRepository,
    ICustomerRepository customerRepository, IBusinessClock businessClock) : ISalesContextFacade
{
    /// <summary>
    ///     Paid sales' totals plus whatever has actually been collected on
    ///     credit sales (see PaymentPlan/InstallmentPayment) — a Credit sale
    ///     itself never contributes its TotalAmount here, only the
    ///     installments actually paid against it, on the day they were paid.
    /// </summary>
    public async Task<decimal> GetTotalRevenue(int businessId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken)
    {
        var paidTotal = await saleRepository.SumPaidTotalByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken);
        var collectedInstallments = await paymentPlanRepository.FindCollectedInstallmentPaymentsByBusinessIdAsync(
            businessId, dateFrom, dateTo, cancellationToken);

        return paidTotal + collectedInstallments.Sum(payment => payment.Amount);
    }

    public async Task<IReadOnlyCollection<(DateOnly Date, decimal Total)>> GetSalesByDay(int businessId, DateOnly dateFrom,
        DateOnly dateTo, CancellationToken cancellationToken)
    {
        var sales = await saleRepository.FindAllByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken);
        var paidByDate = sales.Where(sale => sale.Status == SaleStatus.Paid)
            .GroupBy(sale => businessClock.ToLocalDate(sale.Date))
            .ToDictionary(group => group.Key, group => group.Sum(sale => sale.TotalAmount));

        // A credit sale's own date never contributes here — only the dates
        // its installments were actually collected on, same reasoning as
        // GetTotalRevenue above. A cuota paid three weeks after the sale
        // must show as revenue on the day it was paid, not the day the sale
        // happened.
        var installmentPayments = await paymentPlanRepository.FindCollectedInstallmentPaymentsByBusinessIdAsync(
            businessId, dateFrom, dateTo, cancellationToken);
        var collectedByDate = installmentPayments
            .GroupBy(payment => businessClock.ToLocalDate(payment.PaidAt))
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount));

        return paidByDate.Keys.Union(collectedByDate.Keys)
            .Select(date => (Date: date, Total: paidByDate.GetValueOrDefault(date, 0m) + collectedByDate.GetValueOrDefault(date, 0m)))
            .OrderBy(entry => entry.Date)
            .ToList();
    }

    /// <summary>
    ///     Paid sales AND credit sales — a credit sale used to be invisible
    ///     here entirely (filtered to Paid only), which hid it from the
    ///     bodega's own records the moment it stopped being misreported as
    ///     Paid. CollectedAmount is the sale's own total for a Paid sale, and
    ///     whatever's actually been collected so far for a Credit one.
    /// </summary>
    public async Task<IReadOnlyCollection<SaleExportRow>> GetSalesForExport(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var sales = (await saleRepository.FindAllByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken))
            .Where(sale => sale.Status is SaleStatus.Paid or SaleStatus.Credit)
            .ToList();

        var creditSaleIds = sales.Where(sale => sale.Status == SaleStatus.Credit).Select(sale => sale.Id).ToList();
        var plans = creditSaleIds.Count > 0
            ? await paymentPlanRepository.FindBySaleIdsAsync(creditSaleIds, cancellationToken)
            : [];
        var collectedBySaleId = plans.ToDictionary(plan => plan.SaleId,
            plan => plan.Payments.Where(payment => !payment.IsReversed).Sum(payment => payment.Amount));

        return sales.Select(sale => new SaleExportRow(sale.Id, sale.Date, sale.PaymentMethod, sale.TotalAmount,
                sale.Status == SaleStatus.Credit ? collectedBySaleId.GetValueOrDefault(sale.Id, 0m) : sale.TotalAmount,
                sale.Currency))
            .ToList();
    }

    /// <summary>
    ///     Batched the same way ExpirationAlertSweepService's own lookup is
    ///     (§ its doc comment): one query for plans, one for their sales, one
    ///     for those sales' customers — instead of N+1 queries per plan.
    /// </summary>
    public async Task<IReadOnlyCollection<PendingInstallmentInfo>> GetPendingInstallmentsForDueSweep(CancellationToken cancellationToken)
    {
        var plansWithNextInstallment = (await paymentPlanRepository.FindAllPendingAcrossBusinessesAsync(cancellationToken))
            .Select(plan => (plan, next: plan.Installments.Where(installment => !installment.IsPaid)
                .OrderBy(installment => installment.DueDate).ThenBy(installment => installment.Number)
                .FirstOrDefault()))
            .Where(entry => entry.next != null)
            .ToList();

        var saleIds = plansWithNextInstallment.Select(entry => entry.plan.SaleId).Distinct().ToList();
        var salesById = (await saleRepository.FindAllIgnoringTenantByIdsAsync(saleIds, cancellationToken))
            .ToDictionary(sale => sale.Id);

        var customerIds = salesById.Values.Where(sale => sale.CustomerId != null)
            .Select(sale => sale.CustomerId!.Value).Distinct().ToList();
        var customerNamesById = (await customerRepository.FindAllIgnoringTenantByIdsAsync(customerIds, cancellationToken))
            .ToDictionary(customer => customer.Id, customer => customer.FullName);

        return plansWithNextInstallment.Select(entry =>
        {
            var sale = salesById.GetValueOrDefault(entry.plan.SaleId);
            var customerName = sale?.CustomerId != null ? customerNamesById.GetValueOrDefault(sale.CustomerId.Value) : null;
            return new PendingInstallmentInfo(entry.plan.Id, entry.plan.SaleId, entry.plan.BusinessId, customerName,
                entry.next!.Id, entry.next.DueDate, entry.next.Amount);
        }).ToList();
    }
}
