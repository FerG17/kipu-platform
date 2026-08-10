namespace Bodega.Platform.Sales.Interfaces.Rest.Resources;

public record CreatePaymentPlanResource(int SaleId, int TotalInstallments);
