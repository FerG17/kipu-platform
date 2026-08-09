using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Dashboard.Application.QueryServices;

public interface IReportQueryService
{
    Task<IEnumerable<Report>> Handle(GetAllReportsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken);

    /// <summary>Re-runs the report's live query (using its stored Type/DateFrom/DateTo) and renders it as CSV — never from a stored snapshot.</summary>
    Task<Result<string>> ExportReportAsCsv(int reportId, CancellationToken cancellationToken);
}
