using FluentValidation;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

public class UpdatePaymentInstallmentCommandValidator : AbstractValidator<UpdatePaymentInstallmentCommand>
{
    public UpdatePaymentInstallmentCommandValidator()
    {
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.DueDate).NotEqual(default(DateOnly));
    }
}
