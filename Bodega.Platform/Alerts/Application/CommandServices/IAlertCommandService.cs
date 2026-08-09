using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Alerts.Application.CommandServices;

public interface IAlertCommandService
{
    Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken);
    Task<Result<Alert>> Handle(ResolveAlertCommand command, CancellationToken cancellationToken);
}
