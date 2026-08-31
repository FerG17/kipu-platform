namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record CreatePaymentPlanResource(int SaleId, IReadOnlyList<InstallmentScheduleLineResource> Schedule);

public record InstallmentScheduleLineResource(DateOnly DueDate, decimal Amount);
