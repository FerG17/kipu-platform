namespace Kipu.Platform.Shared.Domain.Model.Services;

/// <summary>
///     Credit-installment due-date business rules (X6 #7/#12) — mirrors
///     ExpirationRules' shape. Unlike expiration, there's a single alert
///     type covering both "coming due soon" and "overdue" (DaysRemaining
///     goes negative once the due date has passed) — a missed cuota doesn't
///     stop mattering just because its date came and went.
/// </summary>
public static class InstallmentDueRules
{
    public const int DueSoonThresholdDays = 7;

    public static bool IsDueSoon(DateOnly dueDate, DateOnly today, int thresholdDays = DueSoonThresholdDays)
    {
        return dueDate.DayNumber - today.DayNumber <= thresholdDays;
    }
}
