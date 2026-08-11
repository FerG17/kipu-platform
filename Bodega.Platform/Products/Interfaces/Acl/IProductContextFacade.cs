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

    /// <summary>
    ///     Decrements inventory after a confirmed sale — called by Sales &amp; POS.
    ///
    ///     Returns whether the stock actually came off. This used to return a
    ///     bare Task and swallow the outcome, which meant a sale could be
    ///     committed while its stock decrement had silently failed, leaving
    ///     inventory permanently out of step with what was sold. The caller is
    ///     expected to abort its transaction when this comes back false.
    /// </summary>
    Task<bool> DecrementStock(int productId, int businessId, int quantity, CancellationToken cancellationToken);

    /// <summary>
    ///     Puts units back after a sale is cancelled — the goods returned to
    ///     the shelf, so the stock has to return with them. Called by Sales &amp;
    ///     POS when a sale is cancelled.
    /// </summary>
    Task<bool> RestoreStock(int productId, int businessId, int quantity, CancellationToken cancellationToken);

    /// <summary>Total stock for a product across every warehouse — used by Sales &amp; POS to validate a line before confirming a sale.</summary>
    Task<int> GetAvailableStock(int productId, CancellationToken cancellationToken);

    /// <summary>
    ///     Registers a real stock intake tagged with a supplier — called by
    ///     Suppliers &amp; Replenishment when a purchase order moves to
    ///     RECEIVED. Uses the business's first warehouse (same known
    ///     limitation as RegisterStockSaleCommand — see InventoryCommandService).
    ///
    ///     Returns whether the goods actually landed in inventory. This used to
    ///     return a bare Task and discard the outcome, exactly like
    ///     DecrementStock once did: an order could be marked RECEIVED and
    ///     committed while its intake had quietly failed, so inventory
    ///     permanently understated what the supplier delivered. The caller is
    ///     expected to abort its transaction when this comes back false.
    /// </summary>
    Task<bool> RegisterStockIntake(int productId, int businessId, int quantity, decimal? purchasePrice, string? supplier,
        string? note, int? supplierId, CancellationToken cancellationToken);

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

    /// <summary>
    ///     Stock movements ("entradas/salidas") for the Dashboard
    ///     "STOCK_MOVEMENTS" report — all four filters are optional and
    ///     combinable. Product names are resolved here so Dashboard never
    ///     needs its own Product lookup.
    ///
    ///     The supplier filter used to live in Dashboard, matching the
    ///     supplier's current name against the free-text name snapshot on
    ///     each movement; renaming a supplier therefore orphaned its whole
    ///     history. It is applied here now, against the stable SupplierId.
    /// </summary>
    Task<IReadOnlyCollection<StockMovementReportLine>> GetStockMovementsForReport(int businessId, DateOnly? dateFrom,
        DateOnly? dateTo, int? productId, int? supplierId, CancellationToken cancellationToken);
}

public record ProductKpisSnapshot(int TotalProducts, int LowStockCount, int ExpiringSoonCount, decimal InventoryValue);

public record TopStockProductInfo(int ProductId, string ProductName, int TotalStock);

public record StockMovementReportLine(int ProductId, string ProductName, string Type, int Quantity, string Supplier,
    string Note, DateTimeOffset RegisteredAt);
