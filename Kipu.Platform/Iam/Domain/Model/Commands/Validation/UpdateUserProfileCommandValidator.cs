using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

/// <summary>No validator existed for this command at all — Name/LastName/Phone reached the database completely unchecked on a profile update.</summary>
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(command => command.Name).MustBeAUserName();
        RuleFor(command => command.LastName).MustBeAUserLastName();
        RuleFor(command => command.Phone).MustBeAUserPhone();
    }
}
