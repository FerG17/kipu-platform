using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Alerts.Interfaces.Acl;
using Bodega.Platform.Shared.Domain.Model.Services;

namespace Bodega.Platform.Alerts.Application.Acl;

public class AlertsContextFacade(IAlertRuleRepository alertRuleRepository) : IAlertsContextFacade
{
    public async Task<int> GetExpirationThresholdDays(int businessId, CancellationToken cancellationToken)
    {
        var rule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.Expiration,
            cancellationToken);

        return rule?.ThresholdValue ?? ExpirationRules.ExpiringSoonThresholdDays;
    }
}
