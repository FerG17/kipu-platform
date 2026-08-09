namespace Bodega.Platform.Products.Domain.Model.Commands;

public record CreateOrUpdateBatchCommand(int ProductId, int BusinessId, DateOnly? Expiration, decimal PurchasePrice, int? InventoryId);
