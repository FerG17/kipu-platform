using Kipu.Platform.Suppliers.Application.QueryServices;
using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Domain.Model.Queries;
using Kipu.Platform.Suppliers.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Application.Internal.QueryServices;

public class SupplierPaymentPlanQueryService(ISupplierPaymentPlanRepository supplierPaymentPlanRepository) : ISupplierPaymentPlanQueryService
{
    public async Task<SupplierPaymentPlan?> Handle(GetSupplierPaymentPlanByPurchaseOrderIdQuery query, CancellationToken cancellationToken)
    {
        return await supplierPaymentPlanRepository.FindByPurchaseOrderIdAsync(query.PurchaseOrderId, cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentPlan>> Handle(GetPendingSupplierPaymentPlansByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await supplierPaymentPlanRepository.FindPendingByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<IEnumerable<SupplierPaymentPlan>> Handle(GetPendingSupplierPaymentPlansBySupplierIdQuery query,
        CancellationToken cancellationToken)
    {
        return await supplierPaymentPlanRepository.FindPendingBySupplierIdAsync(query.SupplierId, cancellationToken);
    }
}
