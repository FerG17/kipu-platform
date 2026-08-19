using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

/// <summary>Manual/technical alert creation (see CreateAlertCommand) — structural validation only, no business-rule checks belong here.</summary>
public class CreateAlertCommandValidator : AbstractValidator<CreateAlertCommand>
{
    public CreateAlertCommandValidator()
    {
        // Lengths mirror the columns in Alerts' ModelBuilderExtensions.
        RuleFor(command => command.ProductName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Type).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Severity).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Message).NotEmpty().MaximumLength(500);
        RuleFor(command => command.CurrentStock).GreaterThanOrEqualTo(0);
        RuleFor(command => command.MinStock).GreaterThanOrEqualTo(0);
    }
}
