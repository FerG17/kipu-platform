using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Kipu.Platform.Suppliers.Interfaces.Rest.Transform;

public static class UpdateSupplierPaymentInstallmentCommandFromResourceAssembler
{
    public static UpdateSupplierPaymentInstallmentCommand ToCommandFromResource(int supplierPaymentPlanId, int installmentId,
        UpdateSupplierPaymentInstallmentResource resource)
    {
        return new UpdateSupplierPaymentInstallmentCommand(supplierPaymentPlanId, installmentId, resource.DueDate, resource.Amount);
    }
}
