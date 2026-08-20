using Kipu.Platform.Alerts.Application.QueryServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Model.Queries;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Alerts.Application.Internal.QueryServices;

public class AlertQueryService(IAlertRepository alertRepository) : IAlertQueryService
{
    public async Task<IEnumerable<Alert>> Handle(GetActiveAlertsByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRepository.FindActiveByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<PagedResult<Alert>> Handle(GetAlertHistoryByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRepository.FindResolvedByBusinessIdAsync(query.BusinessId, query.Page, cancellationToken);
    }
}
