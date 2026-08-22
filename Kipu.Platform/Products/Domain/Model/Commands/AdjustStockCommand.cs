namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>
///     Manual stock correction not tied to a sale — shrinkage, breakage,
///     theft, or fixing a physical count (I25). Delta is signed: negative
///     removes units, positive adds them.
/// </summary>
public record AdjustStockCommand(int ProductId, int WarehouseId, int BusinessId, decimal Delta, string Reason);
