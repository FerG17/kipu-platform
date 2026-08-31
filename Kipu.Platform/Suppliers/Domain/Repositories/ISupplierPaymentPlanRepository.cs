using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Domain.Repositories;

public interface ISupplierPaymentPlanRepository : IBaseRepository<SupplierPaymentPlan>
{
    /// <summary>Unlike the base FindByIdAsync, eager-loads Payments and Installments — see PaymentPlanRepository's own doc comment (Sales, X6 #7) for the same reasoning.</summary>
    Task<SupplierPaymentPlan?> FindByIdWithScheduleAsync(int id, CancellationToken cancellationToken = default);

    Task<SupplierPaymentPlan?> FindByPurchaseOrderIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default);

    Task<IEnumerable<SupplierPaymentPlan>> FindPendingByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Joins against PurchaseOrder to filter by supplier — SupplierPaymentPlan itself only carries PurchaseOrderId.</summary>
    Task<IEnumerable<SupplierPaymentPlan>> FindPendingBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Every non-cancelled, not-fully-paid plan across every business
    ///     (with Installments loaded) — feeds the supplier-installment-due
    ///     alerts sweep, which runs outside any authenticated business. See
    ///     BatchRepository.FindAllActiveAsync for the same IgnoreQueryFilters()
    ///     reasoning.
    /// </summary>
    Task<IEnumerable<SupplierPaymentPlan>> FindAllPendingAcrossBusinessesAsync(CancellationToken cancellationToken = default);
}
