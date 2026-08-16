using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

/// <summary>
///     Shared password strength rule for every command that sets/changes a
///     password (SignUp, InviteUser, ChangePassword) — kept in one place so
///     the policy can't drift between entry points. Deliberately modest (not
///     requiring symbols/mixed case) — this protects real bodega staff, who
///     shouldn't be locked out by a password policy they can't remember.
/// </summary>
public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeAStrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}
