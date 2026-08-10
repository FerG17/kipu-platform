using FluentValidation;

namespace Bodega.Platform.Iam.Domain.Model.Commands.Validation;

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(command => command.Password).MustBeAStrongPassword();
    }
}
