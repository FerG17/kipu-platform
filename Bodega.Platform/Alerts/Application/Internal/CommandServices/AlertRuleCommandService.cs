using FluentValidation;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Alerts.Application.CommandServices;
using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Alerts.Domain.Model.Errors;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Alerts.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.CommandServices;

public class AlertRuleCommandService(
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateOrUpdateAlertRuleCommand> createOrUpdateAlertRuleValidator,
    IStringLocalizer<AlertsMessages> localizer)
    : IAlertRuleCommandService
{
    public async Task<Result<AlertRule>> Handle(CreateOrUpdateAlertRuleCommand command, CancellationToken cancellationToken)
    {
        if (!(await createOrUpdateAlertRuleValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<AlertRule>.Failure(AlertsError.InvalidThreshold, localizer[nameof(AlertsError.InvalidThreshold)]);

        var existing = await alertRuleRepository.FindByBusinessIdAndTypeAsync(command.BusinessId, command.AlertType,
            cancellationToken);

        AlertRule rule;
        if (existing != null)
        {
            existing.UpdateDetails(command.ThresholdValue, command.Enabled);
            alertRuleRepository.Update(existing);
            rule = existing;
        }
        else
        {
            rule = new AlertRule(command.BusinessId, command.AlertType, command.ThresholdValue, command.Enabled);
            await alertRuleRepository.AddAsync(rule, cancellationToken);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<AlertRule>.Success(rule);
    }
}
