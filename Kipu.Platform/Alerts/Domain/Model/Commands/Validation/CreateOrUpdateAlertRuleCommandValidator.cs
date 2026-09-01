using FluentValidation;

namespace Kipu.Platform.Alerts.Domain.Model.Commands.Validation;

public class CreateOrUpdateAlertRuleCommandValidator : AbstractValidator<CreateOrUpdateAlertRuleCommand>
{
    /// <summary>
    ///     Independently configurable rule types. EXPIRATION and EXPIRED are
    ///     NOT the same rule despite both being about expiry:
    ///     AlertExpirationSweepJob.LoadExpirationRules looks up a separate
    ///     AlertRule row for each (own doc comment: "EXPIRATION and EXPIRED
    ///     are separate, independently switchable rules") — a business can
    ///     turn off "warn N days before expiry" while keeping "flag it once
    ///     it's actually expired" on, or vice versa. An earlier pass at this
    ///     whitelist wrongly excluded EXPIRED on the assumption it was
    ///     derived, not configurable — it isn't.
    ///     INSTALLMENT_DUE (X6 #7) and SUPPLIER_INSTALLMENT_DUE (X6 #12) reuse
    ///     this same rule mechanism for their editable due-soon threshold —
    ///     same reasoning as EXPIRATION. (G1 shipped with only INSTALLMENT_DUE
    ///     here, which meant its own threshold couldn't be edited from the UI
    ///     until that omission was caught in live E2E testing — don't repeat
    ///     it for G2.)
    /// </summary>
    private static readonly string[] AllowedAlertTypes =
        ["LOW_STOCK", "OUT_OF_STOCK", "EXPIRATION", "EXPIRED", "INSTALLMENT_DUE", "SUPPLIER_INSTALLMENT_DUE"];

    public CreateOrUpdateAlertRuleCommandValidator()
    {
        // Mirrors the column in Alerts' ModelBuilderExtensions (AlertRule entity).
        // Widened from 20 to 30 for SUPPLIER_INSTALLMENT_DUE (24 chars, X6 #12) —
        // caught by this validator itself rejecting it with 400 before the
        // whitelist check below even ran.
        RuleFor(command => command.AlertType).NotEmpty().MaximumLength(30)
            .Must(type => AllowedAlertTypes.Contains(type))
            .WithMessage($"AlertType must be one of: {string.Join(", ", AllowedAlertTypes)}.");

        // No documented business rule caps this — 365 only rejects absurd
        // input (a typo, a runaway script), not a real "warn a year ahead" use case.
        RuleFor(command => command.ThresholdValue).GreaterThanOrEqualTo(0).LessThanOrEqualTo(365);
    }
}
