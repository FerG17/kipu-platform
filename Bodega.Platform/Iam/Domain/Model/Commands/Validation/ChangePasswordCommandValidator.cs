using FluentValidation;

namespace Bodega.Platform.Iam.Domain.Model.Commands.Validation;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.NewPassword).MustBeAStrongPassword();
    }
}
