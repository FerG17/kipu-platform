namespace Kipu.Platform.Alerts.Interfaces.Rest.Resources;

public record CreateOrUpdateAlertRuleResource(string AlertType, int ThresholdValue, bool Enabled);
