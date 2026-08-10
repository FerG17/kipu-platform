using Bodega.Platform.Dashboard.Domain.Model.Commands;
using Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

namespace Bodega.Platform.Dashboard.Interfaces.Rest.Transform;

public static class GenerateReportCommandFromResourceAssembler
{
    public static GenerateReportCommand ToCommandFromResource(GenerateReportResource resource, int businessId)
    {
        return new GenerateReportCommand(businessId, resource.Type, resource.DateFrom, resource.DateTo, resource.ProductId,
            resource.SupplierId);
    }
}
