namespace Kipu.Platform.Shared.Domain.Model.Services;

/// <summary>
///     Batch-expiration business rules shared by Product (Batch) and Alerts —
///     kept in one place so they're never duplicated/diverge between the two
///     contexts (architecture doc §12 checklist). Expired and expiring-soon
///     are mutually exclusive.
/// </summary>
public static class ExpirationRules
{
    public const int ExpiringSoonThresholdDays = 7;

    public static bool IsExpired(DateOnly? expiration, DateOnly today)
    {
        return expiration.HasValue && expiration.Value.DayNumber - today.DayNumber < 0;
    }

    /// <summary>Between 0 and thresholdDays inclusive, and not already expired.</summary>
    public static bool IsExpiringSoon(DateOnly? expiration, DateOnly today, int thresholdDays = ExpiringSoonThresholdDays)
    {
        if (!expiration.HasValue) return false;
        var daysToExpiry = expiration.Value.DayNumber - today.DayNumber;
        return daysToExpiry >= 0 && daysToExpiry <= thresholdDays;
    }
}
