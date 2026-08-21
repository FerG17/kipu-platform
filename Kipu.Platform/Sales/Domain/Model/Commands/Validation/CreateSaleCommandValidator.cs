using FluentValidation;
using Kipu.Platform.Sales.Domain.Model.Aggregates;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

/// <summary>
///     Guards the quantity and other fields of a sale.
///
///     Without this, the only check a line ever faced was "is there enough
///     stock", which a negative quantity trivially passes — and since
///     Subtotal is Quantity × the product's own BasePrice, a negative
///     quantity would otherwise produce a negative sale total that is then
///     counted as revenue in the dashboard. Discount and UnitPrice are no
///     longer part of this input contract at all (see SaleLineCommand) — the
///     UI never offered discount, and UnitPrice was always silently ignored
///     in favor of the server's own BasePrice, so the only real fix for
///     either was to remove the lever, not cap it.
/// </summary>
public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    /// <summary>The only methods the POS actually offers (see the frontend's PaymentMethod enum) — PaymentMethod is free text on the wire, not a real enum.</summary>
    private static readonly string[] AllowedPaymentMethods =
        [SalePaymentMethod.Cash, SalePaymentMethod.Card, SalePaymentMethod.Yape, SalePaymentMethod.Plin, SalePaymentMethod.Credit];

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
        });

        RuleFor(command => command.PaymentMethod).Must(method => AllowedPaymentMethods.Contains(method));
        RuleFor(command => command.Currency).Must(currency => AllowedCurrencies.Contains(currency));
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
