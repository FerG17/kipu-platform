namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record UpdatePaymentInstallmentResource(DateOnly DueDate, decimal Amount);
