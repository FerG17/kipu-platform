namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record StockMovementResource(
    int Id,
    int ProductId,
    int BusinessId,
    int WarehouseId,
    decimal Quantity,
    string Type,
    string Supplier,
    string Note,
    DateTimeOffset RegisteredAt);
