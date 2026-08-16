using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

/// <summary>Manual/technical alert creation (see CreateAlertCommand) — structural validation only, no business-rule checks belong here.</summary>
public class CreateAlertCommandValidator : AbstractValidator<CreateAlertCommand>
{
    public CreateAlertCommandValidator()
    {
        RuleFor(command => command.ProductName).NotEmpty();
        RuleFor(command => command.Type).NotEmpty();
        RuleFor(command => command.Severity).NotEmpty();
        RuleFor(command => command.Message).NotEmpty();
        RuleFor(command => command.CurrentStock).GreaterThanOrEqualTo(0);
        RuleFor(command => command.MinStock).GreaterThanOrEqualTo(0);
    }
}
