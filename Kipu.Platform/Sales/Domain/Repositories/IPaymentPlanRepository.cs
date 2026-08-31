using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Domain.Repositories;

public interface IPaymentPlanRepository : IBaseRepository<PaymentPlan>
{
    /// <summary>Unlike the base FindByIdAsync, eager-loads Payments — see the implementation's own doc comment.</summary>
    Task<PaymentPlan?> FindByIdWithPaymentsAsync(int id, CancellationToken cancellationToken = default);

    Task<PaymentPlan?> FindBySaleIdAsync(int saleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<PaymentPlan>> FindPendingByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default);

    /// <summary>Joins against Sale to filter by customer — PaymentPlan itself only carries SaleId.</summary>
    Task<IEnumerable<PaymentPlan>> FindPendingByCustomerIdAsync(int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>Every non-reversed installment payment for this business, PaidAt within the range — the source of "collected" revenue for credit sales. See SalesContextFacade.</summary>
    Task<IEnumerable<InstallmentPayment>> FindCollectedInstallmentPaymentsByBusinessIdAsync(int businessId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);

    /// <summary>Plans for a batch of sales (with their Payments loaded) — used to compute "collected so far" per sale for exports.</summary>
    Task<IEnumerable<PaymentPlan>> FindBySaleIdsAsync(IEnumerable<int> saleIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Every non-cancelled, not-fully-paid plan across every business
    ///     (with Installments loaded) — feeds the installment-due alerts
    ///     sweep, which runs outside any authenticated business. See
    ///     BatchRepository.FindAllActiveAsync for the same IgnoreQueryFilters()
    ///     reasoning.
    /// </summary>
    Task<IEnumerable<PaymentPlan>> FindAllPendingAcrossBusinessesAsync(CancellationToken cancellationToken = default);
}
