namespace Bodega.Platform.Dashboard.Domain.Model.Entities;

/// <summary>Example values — not a strict enum, new report types can be added without a migration.</summary>
public static class ReportType
{
    public const string Sales = "SALES";
    public const string Inventory = "INVENTORY";

    /// <summary>Stock intake/sale movements ("entradas/salidas") — the only type that honors ProductId/SupplierId, and the only one exportable as PDF.</summary>
    public const string StockMovements = "STOCK_MOVEMENTS";
}

/// <summary>
///     A generated report's metadata — persisted so history can be listed
///     (decision confirmed §8.3), unlike the frontend's current 100%-in-memory
///     generation. The report's actual figures are never snapshotted here:
///     both generation and export re-run the same live queries against
///     Sales/Product using DateFrom/DateTo, so the numbers are always current
///     as of when they're viewed — this is Report's "Filters" (§6.8) made concrete
///     as real, queryable columns instead of an opaque blob.
///
///     ProductId/SupplierId are only meaningful for ReportType.StockMovements
///     (the other two types ignore them, same as Inventory already ignores
///     DateFrom/DateTo) — kept on the shared entity rather than a subclass
///     since this is metadata, not behavior, and a real subclass hierarchy
///     would be overkill for three optional filter columns.
/// </summary>
public class Report(int businessId, string type, DateOnly? dateFrom, DateOnly? dateTo, int? productId = null,
    int? supplierId = null)
{
    public Report() : this(0, ReportType.Sales, null, null)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Type { get; private set; } = type;
    public DateOnly? DateFrom { get; private set; } = dateFrom;
    public DateOnly? DateTo { get; private set; } = dateTo;
    public int? ProductId { get; private set; } = productId;
    public int? SupplierId { get; private set; } = supplierId;
    public DateTimeOffset GeneratedAt { get; private set; } = DateTimeOffset.UtcNow;
}
