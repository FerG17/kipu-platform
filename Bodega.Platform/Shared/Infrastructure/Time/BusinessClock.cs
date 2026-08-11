using Bodega.Platform.Shared.Application;

namespace Bodega.Platform.Shared.Infrastructure.Time;

/// <summary>
///     Resolves the bodega's local calendar from a single configured time
///     zone (`Business:TimeZone`, defaulting to America/Lima).
///
///     One setting for the whole application rather than a column on
///     Business: this serves a single real shop, and a per-business zone can
///     be introduced later without changing any of the call sites, since they
///     all go through IBusinessClock.
///
///     TimeProvider is injected so tests can freeze "now" at an instant that
///     actually exposes timezone bugs — 02:00 UTC is the previous day in
///     Lima, and that gap is the whole point.
/// </summary>
public class BusinessClock : IBusinessClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public BusinessClock(IConfiguration configuration, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        // Windows and Linux disagree on time zone ids; .NET 8+ resolves IANA
        // ids ("America/Lima") on both, so that is the canonical form here.
        var timeZoneId = configuration.GetValue<string>("Business:TimeZone") ?? "America/Lima";
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    public DateOnly Today => ToLocalDate(_timeProvider.GetUtcNow());

    public DateTimeOffset StartOfDay(DateOnly date)
    {
        return ToInstant(date.ToDateTime(TimeOnly.MinValue));
    }

    public DateTimeOffset EndOfDay(DateOnly date)
    {
        return ToInstant(date.ToDateTime(TimeOnly.MaxValue));
    }

    public DateOnly ToLocalDate(DateTimeOffset instant)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _timeZone).DateTime);
    }

    private DateTimeOffset ToInstant(DateTime localDateTime)
    {
        // A local time can be ambiguous or skipped around a DST change.
        // Peru does not observe DST today, but the conversion is written to
        // survive a zone that does rather than throwing at 2am once a year.
        var offset = _timeZone.IsInvalidTime(localDateTime)
            ? _timeZone.GetUtcOffset(localDateTime.AddHours(1))
            : _timeZone.GetUtcOffset(localDateTime);

        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }
}
