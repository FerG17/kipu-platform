using Bodega.Platform.Sales.Domain.Model.Entities;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class PaymentPlanResourceFromEntityAssembler
{
    public static PaymentPlanResource ToResourceFromEntity(PaymentPlan plan)
    {
        return new PaymentPlanResource(plan.Id, plan.SaleId, plan.BusinessId, plan.TotalInstallments,
            plan.PaidInstallments, plan.IsFullyPaid);
    }
}
