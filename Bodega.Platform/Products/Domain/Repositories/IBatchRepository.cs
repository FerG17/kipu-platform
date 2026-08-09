using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IBatchRepository : IBaseRepository<Batch>
{
    Task<IEnumerable<Batch>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<Batch?> FindActiveByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Batch>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Every ACTIVE batch across every business — used by Alerts' expiration sweep.</summary>
    Task<IEnumerable<Batch>> FindAllActiveAsync(CancellationToken cancellationToken = default);
}
