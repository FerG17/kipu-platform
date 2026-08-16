using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Kipu.Platform.Dashboard.Application.QueryServices;
using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Dashboard.Domain.Model.Errors;
using Kipu.Platform.Dashboard.Domain.Model.Queries;
using Kipu.Platform.Dashboard.Domain.Repositories;
using Kipu.Platform.Dashboard.Infrastructure.Reporting;
using Kipu.Platform.Dashboard.Resources;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Sales.Interfaces.Acl;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Dashboard.Application.Internal.QueryServices;

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
    public async Task<Result<byte[]>> ExportReportAsExcel(int reportId, CancellationToken cancellationToken)
    {
        var report = await reportRepository.FindByIdAsync(reportId, cancellationToken);
        if (report == null) return Result<byte[]>.Failure(DashboardError.ReportNotFound, localizer[nameof(DashboardError.ReportNotFound)]);

        var excel = report.Type switch
        {
            ReportType.Inventory => await BuildInventoryExcel(report, cancellationToken),
            ReportType.StockMovements => await BuildStockMovementsExcel(report, cancellationToken),
            _ => await BuildSalesExcel(report, cancellationToken)
        };

        logger.LogInformation("Report {ReportId} ({ReportType}) exported as Excel for business {BusinessId}",
            report.Id, report.Type, report.BusinessId);

        return Result<byte[]>.Success(excel);
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

    private async Task<byte[]> BuildSalesExcel(Report report, CancellationToken cancellationToken)
    {
        var rows = await salesContextFacade.GetSalesForExport(report.BusinessId, report.DateFrom, report.DateTo, cancellationToken);
        return ExcelReportGenerator.GenerateSales(report.Id, report.DateFrom, report.DateTo, rows);
    }

    private async Task<byte[]> BuildInventoryExcel(Report report, CancellationToken cancellationToken)
    {
        var rows = await productContextFacade.GetTopStockProducts(report.BusinessId, int.MaxValue, cancellationToken);
        return ExcelReportGenerator.GenerateInventory(report.Id, rows);
    }

    private async Task<byte[]> BuildStockMovementsExcel(Report report, CancellationToken cancellationToken)
    {
        var (rows, productName, supplierName) = await GetStockMovementLines(report, cancellationToken);
        return ExcelReportGenerator.GenerateStockMovements(report.Id, report.DateFrom, report.DateTo, productName,
            supplierName, rows);
    }

    /// <summary>
    ///     All three filters are applied in the query now. The supplier one
    ///     used to be applied here in memory, matching the supplier's current
    ///     name against the name snapshot stored on each movement — so
    ///     renaming a supplier silently emptied its report. The name is still
    ///     resolved, but only to label the PDF's filter summary.
    /// </summary>
    private async Task<(IReadOnlyCollection<StockMovementReportLine> Lines, string? ProductName, string? SupplierName)>
        GetStockMovementLines(Report report, CancellationToken cancellationToken)
    {
        var lines = await productContextFacade.GetStockMovementsForReport(report.BusinessId, report.DateFrom, report.DateTo,
            report.ProductId, report.SupplierId, cancellationToken);

        string? supplierName = null;
        if (report.SupplierId.HasValue)
            supplierName = await supplierContextFacade.GetSupplierName(report.SupplierId.Value, cancellationToken);

        // Derived from a matching line rather than a direct lookup (the facade
        // has no "get product name by id" today) — falls back to the raw id
        // so the filter is still shown even when it matched zero movements.
        var productName = report.ProductId.HasValue
            ? lines.Select(line => line.ProductName).FirstOrDefault() ?? $"#{report.ProductId}"
            : null;

        return (lines, productName, supplierName);
    }
}
