namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>
///     Registers a stock intake: sums the quantity if an InventoryItem
///     already exists for (ProductId, WarehouseId), reassigns the product to
///     a different warehouse when explicitly chosen, or creates a new
///     InventoryItem otherwise. Always produces a StockMovement.
/// </summary>
public record RegisterStockIntakeCommand(
    int ProductId,
    int BusinessId,
    int WarehouseId,
    int Quantity,
    decimal? PurchasePrice,
    DateOnly? Expiration,
    string? Supplier,
    string? Note,
    int? MinimumStock,
    int? SupplierId = null);
