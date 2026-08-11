using FluentValidation;

namespace Bodega.Platform.Suppliers.Domain.Model.Commands.Validation;

/// <summary>
///     Guards the money and quantity fields of a purchase order — the mirror
///     of CreateSaleCommandValidator, which the purchases side never had.
///
///     Sales lines were validated; purchase lines went straight from the
///     request body into the order. A negative quantity, a negative unit
///     price, or a discount above 100% all produce a negative line total, and
///     the order's figures are what the owner reconciles against the
///     supplier's invoice. A negative quantity also reached the stock intake
///     on RECEIVED, where it was rejected — and, until it was made to fail the
///     whole transition, rejected silently, leaving an order marked RECEIVED
///     with nothing behind it.
/// </summary>
public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(command => command.Lines).NotEmpty();

        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);

            // Stored as decimal(5,4), i.e. a 0..1 fraction — anything above 1
            // flips the line total negative.
            line.RuleFor(l => l.Discount).InclusiveBetween(0m, 1m);
        });
    }
}
