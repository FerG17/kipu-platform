using FluentValidation;

namespace Kipu.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>Same reasoning as RegisterStockIntakeCommandValidator — an unbounded Delta could overflow StockUnit.</summary>
public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public const int MaxAbsoluteDelta = 1_000_000;

    public AdjustStockCommandValidator()
    {
        // Zero and "required" are still checked separately in
        // InventoryCommandService (they map to their own, more specific
        // errors) — this only adds the bound that was entirely missing.
        RuleFor(command => command.Delta).InclusiveBetween(-MaxAbsoluteDelta, MaxAbsoluteDelta)
            .When(command => command.Delta != 0);
        RuleFor(command => command.Delta).Must(delta => delta == Math.Round(delta, 2))
            .WithMessage("Delta can have at most 2 decimal places.");
        RuleFor(command => command.Reason).MaximumLength(500);
        RuleFor(command => command.NewBatchLabel).MaximumLength(60);
    }
}
