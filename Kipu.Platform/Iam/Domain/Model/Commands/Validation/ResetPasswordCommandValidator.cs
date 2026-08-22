using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.NewPassword).MustBeAStrongPassword();
    }
}
