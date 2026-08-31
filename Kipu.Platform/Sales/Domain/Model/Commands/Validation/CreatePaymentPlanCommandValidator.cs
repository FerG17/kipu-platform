using FluentValidation;

namespace Kipu.Platform.Sales.Domain.Model.Commands.Validation;

public class CreatePaymentPlanCommandValidator : AbstractValidator<CreatePaymentPlanCommand>
{
    public CreatePaymentPlanCommandValidator()
    {
        RuleFor(command => command.Schedule).NotEmpty();
        RuleForEach(command => command.Schedule).ChildRules(schedule =>
        {
            schedule.RuleFor(line => line.Amount).GreaterThan(0);
            schedule.RuleFor(line => line.DueDate).NotEqual(default(DateOnly));
        });
    }
}
