namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record BatchResource(int Id, int ProductId, int BusinessId, DateOnly? Expiration, decimal PurchasePrice, string Status, int? InventoryId);
