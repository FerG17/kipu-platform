namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

/// <summary>ProductId is deliberately not part of this resource — it comes from the route (/products/{id}/stock-intake).</summary>
public record RegisterStockIntakeResource(
    int WarehouseId,
    decimal Quantity,
    decimal? PurchasePrice,
    DateOnly? Expiration,
    string? Supplier,
    string? Note,
    decimal? MinimumStock,
    int? SupplierId = null);
