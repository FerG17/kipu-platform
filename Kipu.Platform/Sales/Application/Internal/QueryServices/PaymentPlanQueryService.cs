using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.QueryServices;

public class PaymentPlanQueryService(IPaymentPlanRepository paymentPlanRepository) : IPaymentPlanQueryService
{
    public async Task<PaymentPlan?> Handle(GetPaymentPlanBySaleIdQuery query, CancellationToken cancellationToken)
    {
        return await paymentPlanRepository.FindBySaleIdAsync(query.SaleId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await paymentPlanRepository.FindPendingByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<IEnumerable<PaymentPlan>> Handle(GetPendingPaymentPlansByCustomerIdQuery query,
        CancellationToken cancellationToken)
    {
        return await paymentPlanRepository.FindPendingByCustomerIdAsync(query.CustomerId, cancellationToken);
    }
}
