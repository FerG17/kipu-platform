using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Alerts.Domain.Model.Queries;

public record GetAlertHistoryByBusinessIdQuery(int BusinessId, PageRequest Page);
