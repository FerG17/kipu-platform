namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>PurchasePrice is null when the caller (e.g. a stock intake that only set an expiration date) didn't mean to touch it — see Batch.UpdateDetails.</summary>
public record CreateOrUpdateBatchCommand(int ProductId, int BusinessId, DateOnly? Expiration, decimal? PurchasePrice, int? InventoryId);
