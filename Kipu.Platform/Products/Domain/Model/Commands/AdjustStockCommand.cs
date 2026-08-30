namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>
///     Manual stock correction not tied to a sale — shrinkage, breakage,
///     theft, or fixing a physical count (I25). Delta is signed: negative
///     removes units, positive adds them.
///
///     See AdjustStockResource for what BatchId/NewBatch* mean (X6 #10) —
///     they only matter when Delta is positive.
/// </summary>
public record AdjustStockCommand(int ProductId, int WarehouseId, int BusinessId, decimal Delta, string Reason,
    int? BatchId = null, DateOnly? NewBatchExpiration = null, decimal? NewBatchPurchasePrice = null,
    string? NewBatchLabel = null);
