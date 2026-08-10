using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Base repository implementing the CRUD operations shared by every
///     bounded-context repository over the same AppDbContext.
/// </summary>
/// <typeparam name="TEntity">The aggregate/entity type the repository manages.</typeparam>
public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Context;

    protected BaseRepository(AppDbContext context)
    {
        Context = context;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    /// <summary>
    ///     Deliberately a filtered LINQ query, not DbSet.FindAsync — Find can
    ///     return an entity straight from the change tracker without
    ///     re-applying global query filters (see AppDbContext's
    ///     BusinessId-scoped filters), which would be a tenant-isolation gap.
    /// </summary>
    public async Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>()
            .FirstOrDefaultAsync(entity => EF.Property<int>(entity, "Id") == id, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        Context.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        Context.Set<TEntity>().Remove(entity);
    }

    public async Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TEntity>().ToListAsync(cancellationToken);
    }
}
