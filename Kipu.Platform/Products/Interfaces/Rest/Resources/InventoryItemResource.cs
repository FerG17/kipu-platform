namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record InventoryItemResource(
    int Id,
    int ProductId,
    int WarehouseId,
    int BusinessId,
    decimal StockUnit,
    decimal MinimumStock,
    DateTimeOffset UpdatedAt);
