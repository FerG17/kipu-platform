namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record AdjustStockResource(int WarehouseId, int Delta, string Reason);
