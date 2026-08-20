using Kipu.Platform.Products.Domain.Model.Entities;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 A6: a pure unit test, no host and no database — InventoryItem.AddStock/
///     AdjustStock use `checked` arithmetic specifically so a StockUnit overflow
///     throws instead of silently wrapping to a negative value that reads as
///     neither low nor out of stock (see StockRules.IsOutOfStock).
/// </summary>
public class InventoryItemOverflowTests
{
    [Fact]
    public void AddStock_ThatWouldOverflowStockUnit_Throws()
    {
        var item = new InventoryItem(productId: 1, warehouseId: 1, businessId: 1, stockUnit: int.MaxValue - 5,
            minimumStock: 0);

        Assert.Throws<OverflowException>(() => item.AddStock(10));
    }

    [Fact]
    public void AdjustStock_ThatWouldOverflowStockUnit_Throws()
    {
        var item = new InventoryItem(productId: 1, warehouseId: 1, businessId: 1, stockUnit: int.MaxValue - 5,
            minimumStock: 0);

        Assert.Throws<OverflowException>(() => item.AdjustStock(10));
    }

    [Fact]
    public void AddStock_WithinBounds_DoesNotThrow()
    {
        var item = new InventoryItem(productId: 1, warehouseId: 1, businessId: 1, stockUnit: 10, minimumStock: 0);

        item.AddStock(5);

        Assert.Equal(15, item.StockUnit);
    }
}
