namespace Bodega.Platform.Shared.Domain.Model.Entities;

/// <summary>
///     Implemented by entities that need automatic CreatedAt/UpdatedAt timestamps,
///     stamped server-side by <see cref="Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors.AuditableEntityInterceptor"/>
///     on every insert/update — never trusted from client input.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
