using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Alerts.Interfaces.Rest.Resources;

namespace Bodega.Platform.Alerts.Interfaces.Rest.Transform;

public static class CreateOrUpdateAlertRuleCommandFromResourceAssembler
{
    public static CreateOrUpdateAlertRuleCommand ToCommandFromResource(CreateOrUpdateAlertRuleResource resource, int businessId)
    {
        return new CreateOrUpdateAlertRuleCommand(businessId, resource.AlertType, resource.ThresholdValue, resource.Enabled);
    }
}
