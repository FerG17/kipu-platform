using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class CreatePaymentPlanCommandFromResourceAssembler
{
    public static CreatePaymentPlanCommand ToCommandFromResource(CreatePaymentPlanResource resource, int businessId)
    {
        return new CreatePaymentPlanCommand(resource.SaleId, businessId, resource.TotalInstallments);
    }
}
