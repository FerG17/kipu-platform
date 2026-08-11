using FluentValidation;

namespace Bodega.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>Field rules for a new catalog entry — see ProductRuleExtensions for why these exist.</summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name).MustBeAProductName();
        RuleFor(command => command.Description).MaximumLength(ProductRuleExtensions.MaxDescriptionLength);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(ProductRuleExtensions.MaxCategoryLength);

        // A negative price feeds straight into the dashboard's "valor de
        // inventario" KPI and into every sale line defaulted from it.
        RuleFor(command => command.BasePrice).MustBeAMoneyAmount();
    }
}
