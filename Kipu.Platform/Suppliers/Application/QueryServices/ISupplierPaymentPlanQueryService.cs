using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Domain.Model.Queries;

namespace Kipu.Platform.Suppliers.Application.QueryServices;

public interface ISupplierPaymentPlanQueryService
{
    Task<SupplierPaymentPlan?> Handle(GetSupplierPaymentPlanByPurchaseOrderIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<SupplierPaymentPlan>> Handle(GetPendingSupplierPaymentPlansByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<SupplierPaymentPlan>> Handle(GetPendingSupplierPaymentPlansBySupplierIdQuery query, CancellationToken cancellationToken);
}
