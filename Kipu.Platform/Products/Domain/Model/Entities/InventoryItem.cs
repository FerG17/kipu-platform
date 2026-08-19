using Kipu.Platform.Shared.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Model.Services;

namespace Kipu.Platform.Products.Domain.Model.Entities;

/// <summary>
///     Current stock of one product at one warehouse. Modeled as a real N:M
///     relation between Product and Warehouse (unique on ProductId+WarehouseId)
///     per architecture doc §8.1 — a business using a single warehouse simply
///     ends up with one row per product, identical behavior to the frontend's
///     original 1:1 model, but a multi-warehouse business (US10, Plan Pro+)
///     is already supported without a future migration.
/// </summary>
public class InventoryItem(int productId, int warehouseId, int businessId, int stockUnit, int minimumStock)
    : IVersionedEntity
{
    public InventoryItem() : this(0, 0, 0, 0, 0)
    {
    }

    public int Id { get; }
    public int ProductId { get; private set; } = productId;
    public int WarehouseId { get; private set; } = warehouseId;
    public int BusinessId { get; private set; } = businessId;
    public int StockUnit { get; private set; } = stockUnit;
    public int MinimumStock { get; private set; } = minimumStock;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Optimistic-concurrency token — this is the row two tills selling the
    ///     same shelf collide on. See <see cref="IVersionedEntity" />.
    /// </summary>
    public int Version { get; set; }

    public bool IsLowStock => StockRules.IsLowStock(StockUnit, MinimumStock);
    public bool IsOutOfStock => StockRules.IsOutOfStock(StockUnit);

    public InventoryItem AddStock(int quantity)
    {
        StockUnit += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public InventoryItem RemoveStock(int quantity)
    {
        StockUnit = Math.Max(0, StockUnit - quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    ///     Manual stock correction (I25) — delta is signed, unlike
    ///     AddStock/RemoveStock. The caller (InventoryCommandService) is
    ///     responsible for rejecting a delta that would take StockUnit
    ///     negative before calling this; unlike RemoveStock, this does not
    ///     silently clamp, since an adjustment that goes further than the
    ///     actual stock allows is exactly the kind of mistake it should be
    ///     rejected for, not truncated.
    /// </summary>
    public InventoryItem AdjustStock(int delta)
    {
        StockUnit += delta;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public InventoryItem ReassignWarehouse(int warehouseId)
    {
        WarehouseId = warehouseId;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public InventoryItem UpdateMinimumStock(int minimumStock)
    {
        MinimumStock = minimumStock;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }
}
