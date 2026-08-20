using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

public class CreateOrUpdateAlertRuleCommandValidator : AbstractValidator<CreateOrUpdateAlertRuleCommand>
{
    /// <summary>
    ///     All 4 AlertType values are independently configurable rule types.
    ///     EXPIRATION and EXPIRED are NOT the same rule despite both being
    ///     about expiry: AlertExpirationSweepJob.LoadExpirationRules looks up
    ///     a separate AlertRule row for each (own doc comment: "EXPIRATION
    ///     and EXPIRED are separate, independently switchable rules") — a
    ///     business can turn off "warn N days before expiry" while keeping
    ///     "flag it once it's actually expired" on, or vice versa. An earlier
    ///     pass at this whitelist wrongly excluded EXPIRED on the assumption
    ///     it was derived, not configurable — it isn't.
    /// </summary>
    private static readonly string[] AllowedAlertTypes = ["LOW_STOCK", "OUT_OF_STOCK", "EXPIRATION", "EXPIRED"];

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
