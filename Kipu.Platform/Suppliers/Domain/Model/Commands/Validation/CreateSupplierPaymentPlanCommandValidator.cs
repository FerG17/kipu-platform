using FluentValidation;

namespace Kipu.Platform.Suppliers.Domain.Model.Commands.Validation;

public class CreateSupplierPaymentPlanCommandValidator : AbstractValidator<CreateSupplierPaymentPlanCommand>
{
    public CreateSupplierPaymentPlanCommandValidator()
    {
        RuleFor(command => command.Schedule).NotEmpty();
        RuleForEach(command => command.Schedule).ChildRules(schedule =>
        {
            schedule.RuleFor(line => line.Amount).GreaterThan(0);
            schedule.RuleFor(line => line.DueDate).NotEqual(default(DateOnly));
        });
    }
}
