using Bodega.Platform.Shared.Domain.Model.Events;

namespace Bodega.Platform.Products.Domain.Model.Events;

/// <summary>
///     Raised whenever an InventoryItem's stock changes (intake or sale).
///     Alerts & Operational Monitoring subscribes to this to re-evaluate
///     low-stock/out-of-stock alerts reactively — see architecture doc §5.4.
/// </summary>
public record StockLevelChangedEvent(
    int ProductId,
    string ProductName,
    int WarehouseId,
    int BusinessId,
    int NewQuantity,
    int MinimumStock) : IEvent;
