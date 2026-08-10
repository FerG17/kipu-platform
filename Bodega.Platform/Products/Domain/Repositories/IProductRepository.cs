using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     For the alerts expiration sweep only (AlertExpirationSweepJob, via
    ///     ProductContextFacade.GetAllActiveBatchesForExpirationSweep), which
    ///     runs outside any authenticated business and needs every business's
    ///     product names at once to label alerts — the regular tenant-scoped
    ///     ListAsync would return nothing there (fail-closed).
    /// </summary>
    Task<IEnumerable<Product>> ListIgnoringTenantAsync(CancellationToken cancellationToken = default);
}
