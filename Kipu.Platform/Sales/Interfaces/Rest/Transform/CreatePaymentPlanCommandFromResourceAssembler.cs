using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class CreatePaymentPlanCommandFromResourceAssembler
{
    public static CreatePaymentPlanCommand ToCommandFromResource(CreatePaymentPlanResource resource, int businessId)
    {
        var schedule = resource.Schedule
            .Select(line => new InstallmentScheduleLine(line.DueDate, line.Amount))
            .ToList();
        return new CreatePaymentPlanCommand(resource.SaleId, businessId, schedule);
    }
}
