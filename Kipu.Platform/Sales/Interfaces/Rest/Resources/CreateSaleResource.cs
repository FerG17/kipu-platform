namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record CreateSaleResource(int? CustomerId, string PaymentMethod, string Currency, string Description,
    IReadOnlyCollection<SaleLineResource> Lines, string? IdempotencyKey = null);
