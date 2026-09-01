namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

/// <summary>Edits an unpaid cuota's date/amount after the plan was created — see SupplierPaymentPlan.FindInstallment (X6 #12).</summary>
public record UpdateSupplierPaymentInstallmentCommand(int SupplierPaymentPlanId, int InstallmentId, DateOnly DueDate, decimal Amount);
