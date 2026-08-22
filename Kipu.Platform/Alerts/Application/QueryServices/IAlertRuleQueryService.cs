using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Alerts.Domain.Model.Queries;

namespace Kipu.Platform.Alerts.Application.QueryServices;

public interface IAlertRuleQueryService
{
    Task<IEnumerable<AlertRule>> Handle(GetAlertRulesByBusinessIdQuery query, CancellationToken cancellationToken);
}
