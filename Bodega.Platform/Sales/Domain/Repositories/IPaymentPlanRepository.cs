using Bodega.Platform.Sales.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Sales.Domain.Repositories;

public interface IPaymentPlanRepository : IBaseRepository<PaymentPlan>
{
    Task<PaymentPlan?> FindBySaleIdAsync(int saleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<PaymentPlan>> FindPendingByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default);

    /// <summary>Joins against Sale to filter by customer — PaymentPlan itself only carries SaleId.</summary>
    Task<IEnumerable<PaymentPlan>> FindPendingByCustomerIdAsync(int customerId,
        CancellationToken cancellationToken = default);
}
