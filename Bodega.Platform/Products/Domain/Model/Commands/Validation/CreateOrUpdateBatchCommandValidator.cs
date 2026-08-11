using FluentValidation;

namespace Bodega.Platform.Products.Domain.Model.Commands.Validation;

public class CreateOrUpdateBatchCommandValidator : AbstractValidator<CreateOrUpdateBatchCommand>
{
    public CreateOrUpdateBatchCommandValidator()
    {
        RuleFor(command => command.PurchasePrice).GreaterThanOrEqualTo(0);
    }
}
