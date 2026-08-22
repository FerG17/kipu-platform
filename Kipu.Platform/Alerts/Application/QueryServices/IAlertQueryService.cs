using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Alerts.Application.QueryServices;

public interface IAlertQueryService
{
    Task<IEnumerable<Alert>> Handle(GetActiveAlertsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<PagedResult<Alert>> Handle(GetAlertHistoryByBusinessIdQuery query, CancellationToken cancellationToken);
}
