using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Dashboard.Interfaces.Rest.Resources;

namespace Kipu.Platform.Dashboard.Interfaces.Rest.Transform;

public static class ReportResourceFromEntityAssembler
{
    public static ReportResource ToResourceFromEntity(Report report)
    {
        return new ReportResource(report.Id, report.BusinessId, report.Type, report.DateFrom, report.DateTo, report.ProductId,
            report.SupplierId, report.GeneratedAt);
    }
}
