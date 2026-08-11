using FluentValidation;

namespace Bodega.Platform.Alerts.Domain.Model.Commands.Validation;

public class CreateOrUpdateAlertRuleCommandValidator : AbstractValidator<CreateOrUpdateAlertRuleCommand>
{
    public CreateOrUpdateAlertRuleCommandValidator()
    {
        RuleFor(command => command.AlertType).NotEmpty();
        RuleFor(command => command.ThresholdValue).GreaterThanOrEqualTo(0);
    }
}
