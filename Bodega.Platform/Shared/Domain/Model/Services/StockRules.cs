namespace Bodega.Platform.Shared.Domain.Model.Services;

/// <summary>
///     Stock-level business rules shared by Product (InventoryItem) and
///     Alerts — kept in one place so they're never duplicated/diverge
///     between the two contexts (architecture doc §12 checklist).
/// </summary>
public static class StockRules
{
    public static bool IsLowStock(int stock, int minimumStock)
    {
        return stock > 0 && stock <= minimumStock;
    }

    public static bool IsOutOfStock(int stock)
    {
        return stock == 0;
    }
}
