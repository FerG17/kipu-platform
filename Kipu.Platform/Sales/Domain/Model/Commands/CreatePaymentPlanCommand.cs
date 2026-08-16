namespace Kipu.Platform.Sales.Domain.Model.Commands;

public record CreatePaymentPlanCommand(int SaleId, int BusinessId, int TotalInstallments);
