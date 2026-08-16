using Kipu.Platform.Alerts.Application.QueryServices;
using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Alerts.Domain.Model.Queries;
using Kipu.Platform.Alerts.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.QueryServices;

public class AlertRuleQueryService(IAlertRuleRepository alertRuleRepository) : IAlertRuleQueryService
{
    public async Task<IEnumerable<AlertRule>> Handle(GetAlertRulesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await alertRuleRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
