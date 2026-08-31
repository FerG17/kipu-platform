namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

/// <summary>One cuota's due date + amount, as entered/edited on the frontend's second screen (X6 #12).</summary>
public record SupplierInstallmentScheduleLine(DateOnly DueDate, decimal Amount);

public record CreateSupplierPaymentPlanCommand(int PurchaseOrderId, int BusinessId, IReadOnlyList<SupplierInstallmentScheduleLine> Schedule);
