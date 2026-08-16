namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record SaleResource(
    int Id,
    int BusinessId,
    int? CustomerId,
    string Status,
    decimal TotalAmount,
    string PaymentMethod,
    DateTimeOffset Date,
    string Description,
    string Currency,
    IReadOnlyCollection<SaleDetailResource> Details);
