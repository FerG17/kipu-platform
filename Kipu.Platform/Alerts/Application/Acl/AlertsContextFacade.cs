using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Alerts.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;

namespace Kipu.Platform.Alerts.Application.Acl;

public class AlertsContextFacade(IAlertRuleRepository alertRuleRepository) : IAlertsContextFacade
{
    public async Task<int> GetExpirationThresholdDays(int businessId, CancellationToken cancellationToken)
    {
        var rule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.Expiration,
            cancellationToken);

        return rule?.ThresholdValue ?? ExpirationRules.ExpiringSoonThresholdDays;
    }
}
