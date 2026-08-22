namespace Kipu.Platform.Shared.Domain.Model.Services;

/// <summary>
///     Stock-level business rules shared by Product (InventoryItem) and
///     Alerts — kept in one place so they're never duplicated/diverge
///     between the two contexts (architecture doc §12 checklist).
/// </summary>
public static class StockRules
{
    public static bool IsLowStock(decimal stock, decimal minimumStock)
    {
        return stock > 0 && stock <= minimumStock;
    }

    /// <summary>
    ///     `&lt;= 0`, not `== 0` — defense in depth. Stock should never go
    ///     negative (every write path is guarded against it), but if
    ///     something corrupt ever slips through, a negative value must still
    ///     read as "out of stock" rather than as neither low nor out, which
    ///     silences every alert for it (see X4 A6).
    /// </summary>
    public static bool IsOutOfStock(decimal stock)
    {
        return stock <= 0;
    }
}
