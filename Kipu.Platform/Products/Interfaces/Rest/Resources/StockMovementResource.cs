namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record StockMovementResource(
    int Id,
    int ProductId,
    int BusinessId,
    int WarehouseId,
    int Quantity,
    string Type,
    string Supplier,
    string Note,
    DateTimeOffset RegisteredAt);
