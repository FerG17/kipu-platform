using System.Text;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Model.Errors;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Dashboard.Domain.Repositories;
using Bodega.Platform.Dashboard.Resources;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Sales.Interfaces.Acl;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Dashboard.Application.Internal.QueryServices;

public class ReportQueryService(
    IReportRepository reportRepository,
    ISalesContextFacade salesContextFacade,
    IProductContextFacade productContextFacade,
    IStringLocalizer<DashboardMessages> localizer)
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

        var csv = report.Type == ReportType.Inventory
            ? await BuildInventoryCsv(report, cancellationToken)
            : await BuildSalesCsv(report, cancellationToken);

        return Result<string>.Success(csv);
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
}
