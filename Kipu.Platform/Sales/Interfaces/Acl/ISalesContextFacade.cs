namespace Kipu.Platform.Sales.Interfaces.Acl;

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

    /// <summary>Every PAID or CREDIT sale within the range, for CSV export — respects the date range (bug fixed per the handoff, §6.8).</summary>
    Task<IReadOnlyCollection<SaleExportRow>> GetSalesForExport(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Each pending credit sale's next unpaid cuota, across every
    ///     business — feeds the installment-due alerts sweep (X6 #7), the
    ///     Sales-side mirror of IProductContextFacade.GetAllActiveBatchesForExpirationSweep.
    /// </summary>
    Task<IReadOnlyCollection<PendingInstallmentInfo>> GetPendingInstallmentsForDueSweep(CancellationToken cancellationToken);
}

/// <summary>CollectedAmount equals TotalAmount for a Paid sale; for a Credit sale it's whatever has actually been collected on its installments so far, which can be less.</summary>
public record SaleExportRow(int SaleId, DateTimeOffset Date, string PaymentMethod, decimal TotalAmount,
    decimal CollectedAmount, string Currency);

/// <summary>CustomerName is null for an anonymous sale (no Customer attached) — the sweep falls back to a generic label.</summary>
public record PendingInstallmentInfo(int PaymentPlanId, int SaleId, int BusinessId, string? CustomerName,
    int InstallmentId, DateOnly DueDate, decimal Amount);
