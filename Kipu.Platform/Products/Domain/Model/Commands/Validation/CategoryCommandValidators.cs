using FluentValidation;

namespace Kipu.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>Field rules for a new category — length mirrors Product.Category's own cap (Products' ModelBuilderExtensions).</summary>
public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(50);
    }
}
