using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Model.Queries;

namespace Bodega.Platform.Alerts.Application.QueryServices;

public interface IAlertQueryService
{
    Task<IEnumerable<Alert>> Handle(GetActiveAlertsByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Alert>> Handle(GetAlertHistoryByBusinessIdQuery query, CancellationToken cancellationToken);
}
