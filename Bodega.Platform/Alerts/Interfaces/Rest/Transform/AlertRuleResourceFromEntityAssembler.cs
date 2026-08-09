using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Alerts.Interfaces.Rest.Resources;

namespace Bodega.Platform.Alerts.Interfaces.Rest.Transform;

public static class AlertRuleResourceFromEntityAssembler
{
    public static AlertRuleResource ToResourceFromEntity(AlertRule rule)
    {
        return new AlertRuleResource(rule.Id, rule.BusinessId, rule.AlertType, rule.ThresholdValue, rule.Enabled);
    }
}
