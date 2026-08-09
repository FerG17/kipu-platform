using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Alerts.Domain.Model.Queries;

namespace Bodega.Platform.Alerts.Application.QueryServices;

public interface IAlertRuleQueryService
{
    Task<IEnumerable<AlertRule>> Handle(GetAlertRulesByBusinessIdQuery query, CancellationToken cancellationToken);
}
