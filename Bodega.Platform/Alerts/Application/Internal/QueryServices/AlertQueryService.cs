using Bodega.Platform.Alerts.Application.QueryServices;
using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Model.Queries;
using Bodega.Platform.Alerts.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository) : IAlertQueryService
{
    public async Task<IEnumerable<Alert>> Handle(GetActiveAlertsByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRepository.FindActiveByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<IEnumerable<Alert>> Handle(GetAlertHistoryByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRepository.FindResolvedByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
