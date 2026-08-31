namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

public record RegisterSupplierInstallmentPaymentCommand(int SupplierPaymentPlanId, int PaidByUserId);
