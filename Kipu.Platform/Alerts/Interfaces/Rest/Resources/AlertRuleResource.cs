namespace Kipu.Platform.Alerts.Interfaces.Rest.Resources;

public record AlertRuleResource(int Id, int BusinessId, string AlertType, int ThresholdValue, bool Enabled);
