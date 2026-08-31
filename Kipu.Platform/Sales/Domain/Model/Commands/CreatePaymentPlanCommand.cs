namespace Kipu.Platform.Sales.Domain.Model.Commands;

/// <summary>One cuota's due date + amount, as entered/edited by the cashier on the frontend's second screen (X6 #7).</summary>
public record InstallmentScheduleLine(DateOnly DueDate, decimal Amount);

public record CreatePaymentPlanCommand(int SaleId, int BusinessId, IReadOnlyList<InstallmentScheduleLine> Schedule);
