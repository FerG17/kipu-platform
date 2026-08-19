using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IProductRepository : IBaseRepository<Product>
{
    Task<IEnumerable<Product>> FindAllByBusinessIdAsync(int businessId, string? category, bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Scoped to the business (not global) — the same barcode could
    ///     legitimately be registered by two unrelated bodegas. Used to
    ///     reject duplicates on create/update (see ProductCommandService).
    /// </summary>
    Task<Product?> FindByBarcodeAsync(int businessId, string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    ///     For the alerts expiration sweep only (AlertExpirationSweepJob, via
    ///     ProductContextFacade.GetAllActiveBatchesForExpirationSweep), which
    ///     runs outside any authenticated business and needs every business's
    ///     product names at once to label alerts — the regular tenant-scoped
    ///     ListAsync would return nothing there (fail-closed).
    /// </summary>
    Task<IEnumerable<Product>> ListIgnoringTenantAsync(CancellationToken cancellationToken = default);
}
