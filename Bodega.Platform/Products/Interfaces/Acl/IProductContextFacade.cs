namespace Bodega.Platform.Products.Interfaces.Acl;

/// <summary>
///     The only way another bounded context may reach into Product &amp;
///     Inventory Management — never direct repository/DbContext access.
/// </summary>
public interface IProductContextFacade
{
    /// <summary>
    ///     Creates the "Almacén Principal" default warehouse for a
    ///     newly-registered business — called by IAM's sign-up flow within
    ///     the same transaction, so account creation stays atomic.
    /// </summary>
    Task<int> CreateDefaultWarehouse(int businessId, CancellationToken cancellationToken);

    /// <summary>Decrements inventory after a confirmed sale — called by Sales &amp; POS.</summary>
    Task DecrementStock(int productId, int businessId, int quantity, CancellationToken cancellationToken);

    /// <summary>Total stock for a product across every warehouse — used by Sales &amp; POS to validate a line before confirming a sale.</summary>
    Task<int> GetAvailableStock(int productId, CancellationToken cancellationToken);

    /// <summary>
    ///     Registers a real stock intake tagged with a supplier — called by
    ///     Suppliers &amp; Replenishment when a purchase order moves to
    ///     RECEIVED. Uses the business's first warehouse (same known
    ///     limitation as RegisterStockSaleCommand — see InventoryCommandService).
    /// </summary>
    Task RegisterStockIntake(int productId, int businessId, int quantity, decimal? purchasePrice, string? supplier,
        string? note, CancellationToken cancellationToken);

    Task<bool> ProductExists(int productId, CancellationToken cancellationToken);

    /// <summary>
    ///     Every ACTIVE batch across every business — used by Alerts'
    ///     AlertExpirationSweepJob to detect products that crossed the
    ///     "about to expire"/"expired" threshold purely by the passage of
    ///     time (no event fires for that on its own — see architecture doc §5.4).
    /// </summary>
    Task<IReadOnlyCollection<ActiveBatchInfo>> GetAllActiveBatchesForExpirationSweep(CancellationToken cancellationToken);

    /// <summary>Live KPI snapshot — computed on demand, never from a stored table (see Dashboard &amp; Analytics, architecture doc §6.8).</summary>
    Task<ProductKpisSnapshot> GetProductKpisSnapshot(int businessId, CancellationToken cancellationToken);

    /// <summary>Ranked by real InventoryItem stock — never by quantity sold.</summary>
    Task<IReadOnlyCollection<TopStockProductInfo>> GetTopStockProducts(int businessId, int count, CancellationToken cancellationToken);
}

public record ProductKpisSnapshot(int TotalProducts, int LowStockCount, int ExpiringSoonCount, decimal InventoryValue);

public record TopStockProductInfo(int ProductId, string ProductName, int TotalStock);
