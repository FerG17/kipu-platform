using Microsoft.Extensions.Localization;
using Bodega.Platform.Alerts.Application.CommandServices;
using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Alerts.Domain.Model.Errors;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Alerts.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.CommandServices;

public class AlertCommandService(
    IAlertRepository alertRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AlertsMessages> localizer)
    : IAlertCommandService
{
    /// <summary>Manual/technical creation — the normal path is the reactive event handlers, not this. See architecture doc §6.5.</summary>
    public async Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = new Alert(command.BusinessId, command.ProductId, command.BatchId, command.ProductName, command.Type,
            command.Severity, command.Message, command.CurrentStock, command.MinStock, command.DaysToExpiry,
            command.WarehouseId);
        await alertRepository.AddAsync(alert, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    public async Task<Result<Alert>> Handle(AcknowledgeAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId, cancellationToken);
        if (alert == null) return Result<Alert>.Failure(AlertsError.AlertNotFound, localizer[nameof(AlertsError.AlertNotFound)]);

        if (alert.Status == AlertStatus.Resolved)
            return Result<Alert>.Failure(AlertsError.AlertAlreadyResolved, localizer[nameof(AlertsError.AlertAlreadyResolved)]);

        alert.Acknowledge();
        alertRepository.Update(alert);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }

    /// <summary>Resolved alerts are pure, immutable history — resolving twice is rejected, not silently accepted.</summary>
    public async Task<Result<Alert>> Handle(ResolveAlertCommand command, CancellationToken cancellationToken)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId, cancellationToken);
        if (alert == null) return Result<Alert>.Failure(AlertsError.AlertNotFound, localizer[nameof(AlertsError.AlertNotFound)]);

        if (alert.Status == AlertStatus.Resolved)
            return Result<Alert>.Failure(AlertsError.AlertAlreadyResolved, localizer[nameof(AlertsError.AlertAlreadyResolved)]);

        alert.Resolve();
        alertRepository.Update(alert);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Alert>.Success(alert);
    }
}
