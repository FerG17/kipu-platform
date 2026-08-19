using FluentValidation;

namespace Kipu.Platform.Iam.Domain.Model.Commands.Validation;

/// <summary>
///     Shared contact-field rules for every command that writes a User's
///     Email/Name/LastName/Phone (SignUp, InviteUser, UpdateUserProfile) —
///     kept in one place so the three entry points can't drift apart. The
///     lengths mirror the columns declared in Iam's ModelBuilderExtensions
///     (User entity); required-ness mirrors it too — Email and Name are
///     NOT NULL there, LastName and Phone are not.
/// </summary>
public static class UserContactRuleExtensions
{
    public const int MaxEmailLength = 255;
    public const int MaxNameLength = 100;
    public const int MaxLastNameLength = 100;
    public const int MaxPhoneLength = 30;

    public static IRuleBuilderOptions<T, string> MustBeAUserEmail<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().MaximumLength(MaxEmailLength).EmailAddress();
    }

    public static IRuleBuilderOptions<T, string> MustBeAUserName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().MaximumLength(MaxNameLength);
    }

    public static IRuleBuilderOptions<T, string> MustBeAUserLastName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.MaximumLength(MaxLastNameLength);
    }

    public static IRuleBuilderOptions<T, string> MustBeAUserPhone<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.MaximumLength(MaxPhoneLength);
    }
}
