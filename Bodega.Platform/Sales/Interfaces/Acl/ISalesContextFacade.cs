namespace Bodega.Platform.Sales.Interfaces.Acl;

/// <summary>
///     The only way another bounded context may reach into Sales &amp; POS
///     Management — never direct repository/DbContext access. Consumed by
///     Dashboard &amp; Analytics for KPIs and reports.
/// </summary>
public interface ISalesContextFacade
{
    /// <summary>Sum of PAID sales' totals — the single source of truth for "total revenue" (architecture doc §6.4).</summary>
    Task<decimal> GetTotalRevenue(int businessId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken);

    /// <summary>Daily totals of PAID sales within the range — for the weekly chart.</summary>
    Task<IReadOnlyCollection<(DateOnly Date, decimal Total)>> GetSalesByDay(int businessId, DateOnly dateFrom, DateOnly dateTo,
        CancellationToken cancellationToken);

    /// <summary>Every PAID sale within the range, for CSV export — respects the date range (bug fixed per the handoff, §6.8).</summary>
    Task<IReadOnlyCollection<SaleExportRow>> GetSalesForExport(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken);
}

public record SaleExportRow(int SaleId, DateTimeOffset Date, string PaymentMethod, decimal TotalAmount, string Currency);
