namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record CreateOrUpdateBatchResource(int ProductId, DateOnly? Expiration, decimal PurchasePrice, int? InventoryId);
