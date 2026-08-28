namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

/// <summary>
///     Delta is decimal, matching AdjustStockCommand — a product sold by
///     weight (X5 Bloque D) needs a fractional adjustment (e.g. -0.5kg
///     spillage). It was typed `int` here until X6 Kardex's "Pérdida rápida"
///     entry point was the first thing to actually exercise this path with a
///     fractional value, surfacing a 400 from ASP.NET's JSON deserializer
///     rejecting a non-integral number into an int — the original per-row
///     "Ajustar stock" button in Inventario carried the same latent bug.
/// </summary>
public record AdjustStockResource(int WarehouseId, decimal Delta, string Reason);
