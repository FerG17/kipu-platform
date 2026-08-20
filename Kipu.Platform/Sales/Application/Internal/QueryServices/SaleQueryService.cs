using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.QueryServices;

public class SaleQueryService(ISaleRepository saleRepository, IPaymentPlanRepository paymentPlanRepository) : ISaleQueryService
{
    public async Task<IEnumerable<Sale>> Handle(GetAllSalesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await saleRepository.FindAllByBusinessIdAsync(query.BusinessId, query.DateFrom, query.DateTo, cancellationToken);
    }

    public async Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken)
    {
        return await saleRepository.FindByIdWithDetailsAsync(query.SaleId, cancellationToken);
    }

    /// <summary>
    ///     Exposed to Admin+Cashier via SalesController (unlike
    ///     DashboardController's own KPI, which is Admin-only) — the POS
    ///     stats bar a cashier sees every shift needs this same figure, and
    ///     it must be the one true calculation, not a second one reimplemented
    ///     client-side (see SalesContextFacade.GetTotalRevenue, which this
    ///     mirrors: Paid sales' totals plus installments actually collected
    ///     on credit sales, never a credit sale's own total).
    /// </summary>
    public async Task<decimal> Handle(GetTotalRevenueByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        var paidTotal = await saleRepository.SumPaidTotalByBusinessIdAsync(query.BusinessId, query.DateFrom, query.DateTo,
            cancellationToken);
        var collectedInstallments = await paymentPlanRepository.FindCollectedInstallmentPaymentsByBusinessIdAsync(
            query.BusinessId, query.DateFrom, query.DateTo, cancellationToken);

        return paidTotal + collectedInstallments.Sum(payment => payment.Amount);
    }
}
