using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Model.Queries;

namespace Kipu.Platform.Sales.Application.QueryServices;

public interface IPaymentPlanQueryService
{
    Task<PaymentPlan?> Handle(GetPaymentPlanBySaleIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByCustomerIdQuery query, CancellationToken cancellationToken);
}
