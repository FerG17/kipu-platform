namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record InventoryItemResource(
    int Id,
    int ProductId,
    int WarehouseId,
    int BusinessId,
    int StockUnit,
    int MinimumStock,
    DateTimeOffset UpdatedAt);
