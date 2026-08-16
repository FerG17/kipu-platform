namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record PaymentPlanResource(int Id, int SaleId, int BusinessId, int TotalInstallments, int PaidInstallments,
    bool IsFullyPaid, bool IsCancelled);
