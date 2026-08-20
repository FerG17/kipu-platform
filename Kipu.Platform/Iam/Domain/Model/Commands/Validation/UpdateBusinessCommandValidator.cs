using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

/// <summary>No validator existed for this command at all — Name/Type/Address/Ruc reached the database completely unchecked on a business profile update.</summary>
public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
{
    /// <summary>
    ///     The only business type the product actually supports today — see
    ///     the frontend's BusinessType enum, whose own doc comment explains
    ///     that the FARMACIA option (and the picker built around it) was
    ///     deliberately removed: this is a single bodega's own system, not a
    ///     multi-vertical SaaS. The backend column has no enum of its own, so
    ///     this is the one place that value is actually enforced.
    /// </summary>
    private static readonly string[] AllowedTypes = ["BODEGA"];

    public UpdateBusinessCommandValidator()
    {
        // Mirrors the columns in Iam's ModelBuilderExtensions (Business entity).
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Type).NotEmpty().MaximumLength(20)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");
        RuleFor(command => command.Address).MaximumLength(255);
        RuleFor(command => command.Ruc).MaximumLength(20);
    }
}
