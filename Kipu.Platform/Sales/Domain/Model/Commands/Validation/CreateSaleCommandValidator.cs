using FluentValidation;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

/// <summary>
///     Guards the money and quantity fields of a sale.
///
///     Without this, the only check a line ever faced was "is there enough
///     stock", which a negative quantity trivially passes. Since Subtotal is
///     Quantity × UnitPrice, a negative quantity or a negative price would
///     otherwise produce a negative sale total that is then counted as
///     revenue in the dashboard. Discount is no longer part of this input
///     contract at all (see SaleLineCommand) — the UI never offered it, so
///     the only real fix was to remove the lever, not cap it.
/// </summary>
public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>The only methods the POS actually offers (see the frontend's PaymentMethod enum) — PaymentMethod is free text on the wire, not a real enum.</summary>
    private static readonly string[] AllowedPaymentMethods = ["CASH", "CARD", "YAPE", "PLIN"];

    /// <summary>The business operates in Soles only — a stray currency here would still get summed into revenue as if it were PEN (see the Dashboard KPI query).</summary>
    private static readonly string[] AllowedCurrencies = ["PEN"];

    /// <summary>No documented business rule caps a sale's size — these only reject absurd input (a typo, a runaway script), not a real bulk sale.</summary>
    private const int MaxLines = 50;
    private const int MaxQuantityPerLine = 1000;

    public CreateSaleCommandValidator()
    {
        RuleFor(command => command.Lines).NotEmpty();
        RuleFor(command => command.Lines).Must(lines => lines.Count <= MaxLines)
            .WithMessage($"A sale can have at most {MaxLines} lines.");

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0).LessThanOrEqualTo(MaxQuantityPerLine);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });

        RuleFor(command => command.PaymentMethod).Must(method => AllowedPaymentMethods.Contains(method));
        RuleFor(command => command.Currency).Must(currency => AllowedCurrencies.Contains(currency));
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
