using Bodega.Platform.Sales.Domain.Model.Entities;
using Bodega.Platform.Sales.Domain.Model.Queries;

namespace Bodega.Platform.Sales.Application.QueryServices;

public interface IPaymentPlanQueryService
{
    Task<PaymentPlan?> Handle(GetPaymentPlanBySaleIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByCustomerIdQuery query, CancellationToken cancellationToken);
}
