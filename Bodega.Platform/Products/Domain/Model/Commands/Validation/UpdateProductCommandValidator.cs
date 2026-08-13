using FluentValidation;

namespace Bodega.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>Same field rules as creation — an edit must not be able to put a product into a state a create would refuse.</summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Name).MustBeAProductName();
        RuleFor(command => command.Description).MaximumLength(ProductRuleExtensions.MaxDescriptionLength);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(ProductRuleExtensions.MaxCategoryLength);
        RuleFor(command => command.BasePrice).MustBeAMoneyAmount();

        RuleFor(command => command.Barcode).MaximumLength(ProductRuleExtensions.MaxBarcodeLength)
            .When(command => !string.IsNullOrEmpty(command.Barcode));
    }
}
