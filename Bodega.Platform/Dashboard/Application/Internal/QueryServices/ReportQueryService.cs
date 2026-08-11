using System.Text;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Model.Errors;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Dashboard.Domain.Repositories;
using Bodega.Platform.Dashboard.Infrastructure.Reporting;
using Bodega.Platform.Dashboard.Resources;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Sales.Interfaces.Acl;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Suppliers.Interfaces.Acl;

namespace Bodega.Platform.Dashboard.Application.Internal.QueryServices;

public class ReportQueryService(
    IReportRepository reportRepository,
    ISalesContextFacade salesContextFacade,
    IProductContextFacade productContextFacade,
    ISupplierContextFacade supplierContextFacade,
    IStringLocalizer<DashboardMessages> localizer,
    ILogger<ReportQueryService> logger)
    : IReportQueryService
{
    public async Task<IEnumerable<Report>> Handle(GetAllReportsByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await reportRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken)
    {
        return await reportRepository.FindByIdAsync(query.ReportId, cancellationToken);
    }

    /// <summary>
    ///     Re-runs the same live query the report's Type/DateFrom/DateTo
    ///     describe — sales figures always respect the stored date range
    ///     (bug fixed per the handoff); inventory figures are always
    ///     "current state" (no historical snapshot exists to filter by date).
    /// </summary>
    public async Task<Result<string>> ExportReportAsCsv(int reportId, CancellationToken cancellationToken)
    {
        var report = await reportRepository.FindByIdAsync(reportId, cancellationToken);
        if (report == null) return Result<string>.Failure(DashboardError.ReportNotFound, localizer[nameof(DashboardError.ReportNotFound)]);

        var csv = report.Type switch
        {
            ReportType.Inventory => await BuildInventoryCsv(report, cancellationToken),
            ReportType.StockMovements => await BuildStockMovementsCsv(report, cancellationToken),
            _ => await BuildSalesCsv(report, cancellationToken)
        };

        logger.LogInformation("Report {ReportId} ({ReportType}) exported as CSV for business {BusinessId}",
            report.Id, report.Type, report.BusinessId);

        return Result<string>.Success(csv);
    }

    /// <summary>PDF is only implemented for STOCK_MOVEMENTS ("entradas/salidas") — the report type this was actually requested for.</summary>
    public async Task<Result<byte[]>> ExportReportAsPdf(int reportId, CancellationToken cancellationToken)
    {
        var report = await reportRepository.FindByIdAsync(reportId, cancellationToken);
        if (report == null) return Result<byte[]>.Failure(DashboardError.ReportNotFound, localizer[nameof(DashboardError.ReportNotFound)]);

        if (report.Type != ReportType.StockMovements)
            return Result<byte[]>.Failure(DashboardError.UnsupportedReportTypeForPdf,
                localizer[nameof(DashboardError.UnsupportedReportTypeForPdf)]);

        var (lines, productName, supplierName) = await GetStockMovementLines(report, cancellationToken);
        var pdf = StockMovementsPdfGenerator.Generate(report.Id, report.DateFrom, report.DateTo, productName, supplierName, lines);

        logger.LogInformation(
            "Report {ReportId} (STOCK_MOVEMENTS) exported as PDF for business {BusinessId}: {LineCount} line(s), {SizeBytes} bytes",
            report.Id, report.BusinessId, lines.Count, pdf.Length);

        return Result<byte[]>.Success(pdf);
    }

    private async Task<string> BuildSalesCsv(Report report, CancellationToken cancellationToken)
    {
        var rows = await salesContextFacade.GetSalesForExport(report.BusinessId, report.DateFrom, report.DateTo, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("SaleId,Date,PaymentMethod,TotalAmount,Currency");
        foreach (var row in rows)
            builder.AppendLine($"{row.SaleId},{row.Date:O},{row.PaymentMethod},{row.TotalAmount},{row.Currency}");

        return builder.ToString();
    }

    private async Task<string> BuildInventoryCsv(Report report, CancellationToken cancellationToken)
    {
        var rows = await productContextFacade.GetTopStockProducts(report.BusinessId, int.MaxValue, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("ProductId,ProductName,CurrentStock");
        foreach (var row in rows)
            builder.AppendLine($"{row.ProductId},{row.ProductName},{row.TotalStock}");

        return builder.ToString();
    }

    private async Task<string> BuildStockMovementsCsv(Report report, CancellationToken cancellationToken)
    {
        var (rows, _, _) = await GetStockMovementLines(report, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Date,ProductId,ProductName,Type,Quantity,Supplier,Note");
        foreach (var row in rows)
            builder.AppendLine(
                $"{row.RegisteredAt:O},{row.ProductId},{row.ProductName},{row.Type},{row.Quantity},{row.Supplier},{row.Note}");

        return builder.ToString();
    }

    /// <summary>
    ///     Applies the third filter (supplier) that IProductContextFacade
    ///     doesn't know about — StockMovement.Supplier is free text, so this
    ///     resolves the supplier's display name via ISupplierContextFacade
    ///     and matches it against that text, rather than joining on an id
    ///     Products has no knowledge of.
    /// </summary>
    private async Task<(IReadOnlyCollection<StockMovementReportLine> Lines, string? ProductName, string? SupplierName)>
        GetStockMovementLines(Report report, CancellationToken cancellationToken)
    {
        var lines = await productContextFacade.GetStockMovementsForReport(report.BusinessId, report.DateFrom, report.DateTo,
            report.ProductId, cancellationToken);

        string? supplierName = null;
        if (report.SupplierId.HasValue)
        {
            supplierName = await supplierContextFacade.GetSupplierName(report.SupplierId.Value, cancellationToken);
            lines = lines.Where(line => string.Equals(line.Supplier, supplierName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Derived from a matching line rather than a direct lookup (the facade
        // has no "get product name by id" today) — falls back to the raw id
        // so the filter is still shown even when it matched zero movements.
        var productName = report.ProductId.HasValue
            ? lines.Select(line => line.ProductName).FirstOrDefault() ?? $"#{report.ProductId}"
            : null;

        return (lines, productName, supplierName);
    }
}
