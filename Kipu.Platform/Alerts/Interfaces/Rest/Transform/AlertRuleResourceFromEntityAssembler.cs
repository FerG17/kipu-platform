using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Alerts.Interfaces.Rest.Resources;

namespace Kipu.Platform.Alerts.Interfaces.Rest.Transform;

public static class AlertRuleResourceFromEntityAssembler
{
    public static AlertRuleResource ToResourceFromEntity(AlertRule rule)
    {
        return new AlertRuleResource(rule.Id, rule.BusinessId, rule.AlertType, rule.ThresholdValue, rule.Enabled);
    }
}
