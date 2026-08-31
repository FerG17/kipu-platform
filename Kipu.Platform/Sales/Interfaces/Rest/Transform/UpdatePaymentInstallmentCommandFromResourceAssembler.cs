using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class UpdatePaymentInstallmentCommandFromResourceAssembler
{
    public static UpdatePaymentInstallmentCommand ToCommandFromResource(int paymentPlanId, int installmentId,
        UpdatePaymentInstallmentResource resource)
    {
        return new UpdatePaymentInstallmentCommand(paymentPlanId, installmentId, resource.DueDate, resource.Amount);
    }
}
