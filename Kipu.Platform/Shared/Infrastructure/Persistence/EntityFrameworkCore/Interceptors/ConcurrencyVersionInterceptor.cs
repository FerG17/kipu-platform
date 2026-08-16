using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Kipu.Platform.Shared.Domain.Model.Entities;

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

/// <summary>
///     Bumps <see cref="IVersionedEntity.Version" /> on every modified row
///     right before SaveChanges writes it, exactly like
///     <see cref="AuditableEntityInterceptor" /> stamps UpdatedAt.
///
///     EF Core builds the UPDATE's WHERE clause from the property's *original*
///     value (the one loaded from the database) while writing the incremented
///     one, which is what makes the write conditional on nothing else having
///     touched the row in the meantime. Doing it here rather than inside each
///     aggregate means a newly added domain method cannot forget to do it and
///     silently reopen the race.
/// </summary>
public class ConcurrencyVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        BumpModifiedEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        BumpModifiedEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void BumpModifiedEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IVersionedEntity>())
            if (entry.State == EntityState.Modified)
                entry.Entity.Version++;
    }
}
