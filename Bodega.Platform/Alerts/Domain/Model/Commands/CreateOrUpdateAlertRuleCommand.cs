namespace Bodega.Platform.Alerts.Domain.Model.Commands;

public record CreateOrUpdateAlertRuleCommand(int BusinessId, string AlertType, int ThresholdValue, bool Enabled);
