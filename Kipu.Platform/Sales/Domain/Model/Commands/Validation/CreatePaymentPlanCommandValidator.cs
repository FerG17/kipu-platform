using FluentValidation;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

public class CreatePaymentPlanCommandValidator : AbstractValidator<CreatePaymentPlanCommand>
{
    public CreatePaymentPlanCommandValidator()
    {
        RuleFor(command => command.TotalInstallments).GreaterThanOrEqualTo(1);
    }
}
