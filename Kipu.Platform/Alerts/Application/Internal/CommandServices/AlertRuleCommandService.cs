using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Domain.Model.Commands;
using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Alerts.Domain.Model.Errors;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Alerts.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.CommandServices;

public class AlertRuleCommandService(
    IAlertRuleRepository alertRuleRepository,
    IAlertRepository alertRepository,
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

        // Turning a rule off should stop demanding attention right away —
        // otherwise its alerts sit ACTIVE until the next scheduled sweep
        // (AlertExpirationSweepJob only runs every Alerts:SweepIntervalHours,
        // default 6h) even though the owner just said this type no longer
        // matters to them.
        if (!command.Enabled)
        {
            var alertsOfType = (await alertRepository.FindActiveByBusinessIdAsync(command.BusinessId, cancellationToken))
                .Where(alert => alert.Type == command.AlertType);
            foreach (var alert in alertsOfType)
            {
                alert.Resolve();
                alertRepository.Update(alert);
            }
        }

        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<AlertRule>.Success(rule);
    }
}
