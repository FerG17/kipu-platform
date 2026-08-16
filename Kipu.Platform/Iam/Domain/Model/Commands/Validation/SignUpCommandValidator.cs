using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(command => command.Password).MustBeAStrongPassword();
    }
}
