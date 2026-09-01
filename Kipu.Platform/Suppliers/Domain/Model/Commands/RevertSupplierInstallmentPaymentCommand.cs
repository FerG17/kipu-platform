namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

/// <summary>Undoes the most recently registered payment on a plan — see SupplierPaymentPlan.RevertLastPayment.</summary>
public record RevertSupplierInstallmentPaymentCommand(int SupplierPaymentPlanId, int RevertedByUserId);
