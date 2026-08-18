using System.Data;

namespace Kipu.Platform.Shared.Domain.Repositories;

/// <summary>
///     Commits the current change set to the database. A single instance per
///     request, shared by every repository (they all write through the same
///     AppDbContext), so one command handler's writes commit atomically.
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts an explicit database transaction spanning multiple
    ///     CompleteAsync calls. Needed by command handlers that must persist in
    ///     more than one round-trip — e.g. resolving a circular FK between two
    ///     aggregates by inserting one, then the other, then patching the
    ///     first — while still guaranteeing all-or-nothing atomicity.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Same as <see cref="BeginTransactionAsync(CancellationToken)" />,
    ///     but at an explicit isolation level — used by check-then-write
    ///     guards where a plain transaction isn't enough. MySQL's default
    ///     (REPEATABLE READ) still lets two concurrent transactions each read
    ///     "the guard passes" before either commits (e.g. two admins each
    ///     confirming a third admin still exists, then both proceeding);
    ///     Serializable forces the second one to fail instead of silently
    ///     racing past the same check the first one just made.
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
}

/// <summary>
///     Thin abstraction over the underlying persistence transaction, so
///     Application-layer command handlers never depend on an EF Core type
///     directly.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
