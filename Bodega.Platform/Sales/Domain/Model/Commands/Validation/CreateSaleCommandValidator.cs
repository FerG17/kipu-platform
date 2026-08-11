using FluentValidation;

namespace Bodega.Platform.Sales.Domain.Model.Commands.Validation;

/// <summary>
///     Guards the money and quantity fields of a sale.
///
///     Without this, the only check a line ever faced was "is there enough
///     stock", which a negative quantity trivially passes. Since Subtotal is
///     Quantity × UnitPrice × (1 - Discount), a negative quantity, a negative
///     price, or a discount above 100% all produce a negative sale total that
///     is then counted as revenue in the dashboard.
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

            // Stored as decimal(5,4), i.e. a 0..1 fraction — anything above 1
            // flips the subtotal negative.
            line.RuleFor(l => l.Discount).InclusiveBetween(0m, 1m);
        });
    }
}
