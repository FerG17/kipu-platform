using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

public class CreateOrUpdateAlertRuleCommandValidator : AbstractValidator<CreateOrUpdateAlertRuleCommand>
{
    /// <summary>
    ///     Only these 3 AlertType values are configurable rule types — EXPIRED
    ///     (see Alert.AlertType) is derived from an EXPIRATION alert's date
    ///     passing, not an independently configurable rule (see AlertRule's
    ///     own doc comment and the frontend's hardcoded rule list).
    /// </summary>
    private static readonly string[] AllowedAlertTypes = ["LOW_STOCK", "OUT_OF_STOCK", "EXPIRATION"];

    public CreateOrUpdateAlertRuleCommandValidator()
    {
        // Mirrors the column in Alerts' ModelBuilderExtensions (AlertRule entity).
        RuleFor(command => command.AlertType).NotEmpty().MaximumLength(20)
            .Must(type => AllowedAlertTypes.Contains(type))
            .WithMessage($"AlertType must be one of: {string.Join(", ", AllowedAlertTypes)}.");

        // No documented business rule caps this — 365 only rejects absurd
        // input (a typo, a runaway script), not a real "warn a year ahead" use case.
        RuleFor(command => command.ThresholdValue).GreaterThanOrEqualTo(0).LessThanOrEqualTo(365);
    }
}
