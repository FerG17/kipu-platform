using Bodega.Platform.Alerts.Application.QueryServices;
using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Alerts.Domain.Model.Queries;
using Bodega.Platform.Alerts.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.QueryServices;

public class AlertRuleQueryService(IAlertRuleRepository alertRuleRepository) : IAlertRuleQueryService
{
    public async Task<IEnumerable<AlertRule>> Handle(GetAlertRulesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRuleRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
