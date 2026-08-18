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
    public CreateSaleCommandValidator()
    {
        RuleFor(command => command.Lines).NotEmpty();

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
