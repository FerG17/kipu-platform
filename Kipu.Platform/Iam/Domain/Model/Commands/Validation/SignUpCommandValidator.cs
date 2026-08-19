using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(command => command.Password).MustBeAStrongPassword();
        RuleFor(command => command.Email).MustBeAUserEmail();
        RuleFor(command => command.Name).MustBeAUserName();
        RuleFor(command => command.LastName).MustBeAUserLastName();
        RuleFor(command => command.Phone).MustBeAUserPhone();
    }
}
