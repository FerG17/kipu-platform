namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

/// <summary>
///     Delta is decimal, matching AdjustStockCommand — a product sold by
///     weight (X5 Bloque D) needs a fractional adjustment (e.g. -0.5kg
///     spillage). It was typed `int` here until X6 Kardex's "Pérdida rápida"
///     entry point was the first thing to actually exercise this path with a
///     fractional value, surfacing a 400 from ASP.NET's JSON deserializer
///     rejecting a non-integral number into an int — the original per-row
///     "Ajustar stock" button in Inventario carried the same latent bug.
///
///     X6 #10 — a positive Delta must always land in a specific lot, closing
///     the integrity gap where an adjustment only ever touched the aggregate
///     total: BatchId credits an existing lot the owner picked; when it's
///     null, NewBatch* (all optional — a lot needs none of them to exist)
///     opens a brand-new one instead. A negative Delta ignores all of these —
///     removal is always automatic FEFO across active lots, same as a sale.
/// </summary>
public record AdjustStockResource(int WarehouseId, decimal Delta, string Reason, int? BatchId = null,
    DateOnly? NewBatchExpiration = null, decimal? NewBatchPurchasePrice = null, string? NewBatchLabel = null);
