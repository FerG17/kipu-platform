using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

namespace Bodega.Platform.Dashboard.Interfaces.Rest.Transform;

public static class ReportResourceFromEntityAssembler
{
    public static ReportResource ToResourceFromEntity(Report report)
    {
        return new ReportResource(report.Id, report.BusinessId, report.Type, report.DateFrom, report.DateTo, report.GeneratedAt);
    }
}
