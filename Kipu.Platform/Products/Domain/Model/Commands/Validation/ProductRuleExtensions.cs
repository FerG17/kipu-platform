using FluentValidation;

namespace Kipu.Platform.Products.Domain.Model.Commands.Validation;

/// <summary>
///     Shared field rules for the product catalog, kept in one place so create
///     and update can't drift apart — the same reasoning as IAM's
///     PasswordRuleExtensions.
///
///     The lengths here are not arbitrary: they mirror the columns declared in
///     Products' ModelBuilderExtensions. Nothing checked them before, so a
///     name longer than its varchar(150) travelled all the way to MySQL and
///     came back as an unhandled DbUpdateException — a 500 for what is plainly
///     a bad request, and the same defect class the previous audit fixed for
///     an invalid RoleId. A blank name passed too: IsRequired() rejects NULL,
///     not "".
/// </summary>
public static class ProductRuleExtensions
{
    /// <summary>decimal(10,2) — eight integer digits, so anything from 10^8 up is out of range at the column.</summary>
    public const decimal MaxMoney = 99_999_999.99m;

    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 500;
    public const int MaxCategoryLength = 50;
    public const int MaxBarcodeLength = 64;

    public static IRuleBuilderOptions<T, string> MustBeAProductName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.NotEmpty().MaximumLength(MaxNameLength);
    }

    public static IRuleBuilderOptions<T, decimal> MustBeAMoneyAmount<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0m).LessThanOrEqualTo(MaxMoney);
    }
}
