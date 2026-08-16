namespace Kipu.Platform.Shared.Domain.Model.Entities;

/// <summary>
///     Implemented by entities where two concurrent requests routinely fight
///     over the same row, and the loser must be told so instead of silently
///     overwriting the winner.
///
///     Every one of those flows is a read-check-write: load the row, decide
///     ("is there enough stock?", "is this sale still PAID?"), then save. The
///     decision is made against a value that another request can change before
///     the save lands, so the check guarantees nothing on its own. The audit
///     proved all three variants were live: eight simultaneous sales of the
///     last unit were all confirmed, cancelling one sale eight times returned
///     its units eight times, and receiving one purchase order eight times
///     booked the delivery eight times.
///
///     <see cref="Version" /> closes that window. It is mapped as an EF Core
///     concurrency token, so every UPDATE carries "AND version = &lt;the value I
///     read&gt;" and is bumped by
///     <see cref="Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors.ConcurrencyVersionInterceptor" />.
///     Whoever commits first moves the version; everyone else matches zero
///     rows and gets a DbUpdateConcurrencyException, which the command
///     services turn into a 409 and a rolled-back transaction.
///
///     Kept out of the aggregates' own mutators on purpose: a version that
///     each domain method has to remember to increment is one an added method
///     will eventually forget, and the gap would be invisible until it costs
///     someone real stock.
/// </summary>
public interface IVersionedEntity
{
    int Version { get; set; }
}
