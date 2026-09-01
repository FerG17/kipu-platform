using FluentValidation;

namespace Kipu.Platform.Suppliers.Domain.Model.Commands.Validation;

public class UpdateSupplierPaymentInstallmentCommandValidator : AbstractValidator<UpdateSupplierPaymentInstallmentCommand>
{
    public UpdateSupplierPaymentInstallmentCommandValidator()
    {
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.DueDate).NotEqual(default(DateOnly));
    }
}
