using FluentValidation;

namespace Bodega.Platform.Iam.Domain.Model.Commands.Validation;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(command => command.Password).MustBeAStrongPassword();
    }
}
