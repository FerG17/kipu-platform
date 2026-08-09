namespace Bodega.Platform.Shared.Domain.Repositories;

/// <summary>
///     Basic CRUD contract every bounded-context repository builds on.
/// </summary>
/// <typeparam name="TEntity">The aggregate/entity type the repository manages.</typeparam>
public interface IBaseRepository<TEntity> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}
