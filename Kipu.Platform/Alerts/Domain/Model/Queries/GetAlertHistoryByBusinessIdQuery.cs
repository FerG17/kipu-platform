namespace Kipu.Platform.Alerts.Domain.Model.Queries;

/// <summary>RESOLVED alerts — immutable history.</summary>
public record GetAlertHistoryByBusinessIdQuery(int BusinessId);
