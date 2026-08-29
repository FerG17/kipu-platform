using FluentValidation;

namespace Kipu.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>
///     Nothing bounded a stock intake before this — only "not negative" (see
///     InventoryCommandService.Handle). Two large-but-individually-plausible
///     intakes could push InventoryItem.StockUnit (a plain int) past
///     int.MaxValue and wrap it to negative, silencing every LOW_STOCK/
///     OUT_OF_STOCK alert for that product (see StockRules.IsOutOfStock,
///     fixed separately to also catch a negative value defensively). This
///     validator is the actual fix: reject a quantity no real bodega intake
///     would ever have, same reasoning as CreateSaleCommandValidator's
///     MaxQuantityPerLine.
/// </summary>
public class RegisterStockIntakeCommandValidator : AbstractValidator<RegisterStockIntakeCommand>
{
    public const int MaxQuantity = 1_000_000;

    public RegisterStockIntakeCommandValidator()
    {
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0).LessThanOrEqualTo(MaxQuantity);
        // Whether a fractional value is actually allowed for this product
        // depends on its UnitOfSale (checked in InventoryCommandService,
        // which already loads the Product) — this only bounds precision to
        // what inventory_items.stock_unit (decimal(10,2)) can hold.
        RuleFor(command => command.Quantity).Must(quantity => quantity == Math.Round(quantity, 2))
            .WithMessage("Quantity can have at most 2 decimal places.");
        RuleFor(command => command.Supplier).MaximumLength(150);
        RuleFor(command => command.Note).MaximumLength(500);
        RuleFor(command => command.Label).MaximumLength(60);
    }
}
