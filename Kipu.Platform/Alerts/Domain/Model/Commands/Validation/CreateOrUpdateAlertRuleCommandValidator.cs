using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

public class CreateOrUpdateAlertRuleCommandValidator : AbstractValidator<CreateOrUpdateAlertRuleCommand>
{
    public CreateOrUpdateAlertRuleCommandValidator()
    {
        // Mirrors the column in Alerts' ModelBuilderExtensions (AlertRule entity).
        RuleFor(command => command.AlertType).NotEmpty().MaximumLength(20);
        RuleFor(command => command.ThresholdValue).GreaterThanOrEqualTo(0);
    }
}
