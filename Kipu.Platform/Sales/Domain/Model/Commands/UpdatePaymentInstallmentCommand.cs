namespace Kipu.Platform.Sales.Domain.Model.Commands;

/// <summary>Edits an unpaid cuota's date/amount after the plan was created — see PaymentPlan.FindInstallment (X6 #7, decision 5).</summary>
public record UpdatePaymentInstallmentCommand(int PaymentPlanId, int InstallmentId, DateOnly DueDate, decimal Amount);
