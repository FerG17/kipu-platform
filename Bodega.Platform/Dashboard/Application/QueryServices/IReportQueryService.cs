using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Dashboard.Application.QueryServices;

public interface IReportQueryService
{
    Task<IEnumerable<Report>> Handle(GetAllReportsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken);

    /// <summary>Re-runs the report's live query (using its stored Type/DateFrom/DateTo) and renders it as a formatted .xlsx workbook — never from a stored snapshot.</summary>
    Task<Result<byte[]>> ExportReportAsExcel(int reportId, CancellationToken cancellationToken);

    /// <summary>Same live re-run, rendered as PDF via QuestPDF — only supported for ReportType.StockMovements.</summary>
    Task<Result<byte[]>> ExportReportAsPdf(int reportId, CancellationToken cancellationToken);
}
