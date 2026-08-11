using FluentValidation;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Products.Interfaces.Acl;
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
    IValidator<CreateAlertCommand> createAlertValidator,
    IProductContextFacade productContextFacade,
    IStringLocalizer<AlertsMessages> localizer)
    : IAlertCommandService
{
    /// <summary>Manual/technical creation — the normal path is the reactive event handlers, not this. See architecture doc §6.5.</summary>
    public async Task<Result<Alert>> Handle(CreateAlertCommand command, CancellationToken cancellationToken)
    {
        if (!(await createAlertValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Alert>.Failure(AlertsError.InvalidAlertData, localizer[nameof(AlertsError.InvalidAlertData)]);

        // ProductId arrived straight from the request body with no ownership
        // check — the only command service here that skipped it. An admin
        // could name another business's product, and because the alert lookup
        // ignores query filters (a product id is globally unique, so the
        // match is exact), that other business's own alert handler would then
        // find this row as the "existing" one and refresh it instead of
        // creating theirs — quietly suppressing their alert.
        if (!await productContextFacade.ProductExists(command.ProductId, cancellationToken))
            return Result<Alert>.Failure(AlertsError.ProductNotFound, localizer[nameof(AlertsError.ProductNotFound)]);

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
