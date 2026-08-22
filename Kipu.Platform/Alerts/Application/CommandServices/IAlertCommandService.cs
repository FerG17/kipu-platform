using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Alerts.Application.CommandServices;

public interface IAlertCommandService
{
    Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(ResolveAlertCommand command, CancellationToken cancellationToken);
}
