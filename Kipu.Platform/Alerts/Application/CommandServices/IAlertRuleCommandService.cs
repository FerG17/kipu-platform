using Kipu.Platform.Alerts.Domain.Model.Commands;
using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Alerts.Application.CommandServices;

public interface IAlertRuleCommandService
{
    Task<Result<AlertRule>> Handle(CreateOrUpdateAlertRuleCommand command, CancellationToken cancellationToken);
}
