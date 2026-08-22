using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Kipu.Platform.Shared.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Commits the current change set via AppDbContext.SaveChangesAsync.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransaction(transaction);
    }

    public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfTransaction(transaction);
    }

    private class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
