using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Kipu.Platform.Suppliers.Interfaces.Rest.Transform;

public static class CreateSupplierPaymentPlanCommandFromResourceAssembler
{
    public static CreateSupplierPaymentPlanCommand ToCommandFromResource(CreateSupplierPaymentPlanResource resource, int businessId)
    {
        var schedule = resource.Schedule
            .Select(line => new SupplierInstallmentScheduleLine(line.DueDate, line.Amount))
            .ToList();
        return new CreateSupplierPaymentPlanCommand(resource.PurchaseOrderId, businessId, schedule);
    }
}
