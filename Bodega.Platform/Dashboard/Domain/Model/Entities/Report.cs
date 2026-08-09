namespace Bodega.Platform.Dashboard.Domain.Model.Entities;

/// <summary>Example values — not a strict enum, new report types can be added without a migration.</summary>
public static class ReportType
{
    public const string Sales = "SALES";
    public const string Inventory = "INVENTORY";
}

/// <summary>
///     A generated report's metadata — persisted so history can be listed
///     (decision confirmed §8.3), unlike the frontend's current 100%-in-memory
///     generation. The report's actual figures are never snapshotted here:
///     both generation and export re-run the same live queries against
///     Sales/Product using DateFrom/DateTo, so the numbers are always current
///     as of when they're viewed — this is Report's "Filters" (§6.8) made concrete
///     as real, queryable columns instead of an opaque blob.
/// </summary>
public class Report(int businessId, string type, DateOnly? dateFrom, DateOnly? dateTo)
{
    public Report() : this(0, ReportType.Sales, null, null)
    {
    }

    public int Id { get; }
    public int BusinessId { get; private set; } = businessId;
    public string Type { get; private set; } = type;
    public DateOnly? DateFrom { get; private set; } = dateFrom;
    public DateOnly? DateTo { get; private set; } = dateTo;
    public DateTimeOffset GeneratedAt { get; private set; } = DateTimeOffset.UtcNow;
}
