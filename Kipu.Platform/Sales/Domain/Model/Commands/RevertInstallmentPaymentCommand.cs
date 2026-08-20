namespace Kipu.Platform.Sales.Domain.Model.Commands;

/// <summary>Undoes the most recently registered payment on a plan — see PaymentPlan.RevertLastPayment.</summary>
public record RevertInstallmentPaymentCommand(int PaymentPlanId, int RevertedByUserId);
