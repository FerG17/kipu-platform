namespace Kipu.Platform.Shared.Application;

/// <summary>
///     Calendar time as the bodega experiences it, rather than as the server
///     happens to be configured.
///
///     Instants (when a sale happened, when a row was written) stay in UTC —
///     that part was always right. What was wrong was deriving *calendar
///     dates* from UTC: the bodega runs in UTC-5, so from 19:00 local
///     onwards UTC has already rolled over to the next day. That made a
///     batch expiring today get rejected as "already expired", pushed the
///     evening's sales into tomorrow's report so the till never matched, and
///     shifted every days-to-expiry count by one.
///
///     Anything that answers "what day is it" or "which instants belong to
///     this day" must go through here.
/// </summary>
public interface IBusinessClock
{
    /// <summary>The current date on the bodega's wall calendar.</summary>
    DateOnly Today { get; }

    /// <summary>The UTC instant at which the given local day begins (its 00:00:00).</summary>
    DateTimeOffset StartOfDay(DateOnly date);

    /// <summary>The UTC instant at which the given local day ends (its last tick before midnight).</summary>
    DateTimeOffset EndOfDay(DateOnly date);

    /// <summary>The local calendar date an instant falls on — e.g. to group sales into the day the bodega counted them.</summary>
    DateOnly ToLocalDate(DateTimeOffset instant);
}
