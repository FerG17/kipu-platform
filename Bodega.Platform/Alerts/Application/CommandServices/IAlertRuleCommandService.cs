using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Alerts.Application.CommandServices;

public interface IAlertRuleCommandService
{
    Task<Result<AlertRule>> Handle(CreateOrUpdateAlertRuleCommand command, CancellationToken cancellationToken);
}
