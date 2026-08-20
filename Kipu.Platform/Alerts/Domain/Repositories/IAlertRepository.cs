using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Domain.Repositories;

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<PagedResult<Alert>> FindResolvedByBusinessIdAsync(int businessId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds a non-resolved alert of the given type for a product (and
    ///     batch, or warehouse, when relevant) — used to decide upsert-vs-create.
    ///     LOW_STOCK/OUT_OF_STOCK pass warehouseId (batchId null); EXPIRATION/
    ///     EXPIRED pass batchId (warehouseId null) — each alert is scoped to
    ///     whichever dimension actually varies for that type.
    /// </summary>
    Task<Alert?> FindActiveByProductAndTypeAsync(int productId, string type, int? batchId, int? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Every still-open alert raised for a batch — closed when the batch is discarded.</summary>
    Task<IEnumerable<Alert>> FindActiveByBatchIdAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Every still-open EXPIRATION/EXPIRED alert across the given batches,
    ///     in one query — used by AlertExpirationSweepJob to look up existing
    ///     alerts for a whole business's batches at once instead of two
    ///     queries per batch (FindActiveByProductAndTypeAsync). Ignores query
    ///     filters for the same reason that method does: the sweep runs
    ///     outside any authenticated business.
    /// </summary>
    Task<IEnumerable<Alert>> FindActiveExpirationAlertsByBatchIdsAsync(IReadOnlyCollection<int> batchIds,
        CancellationToken cancellationToken = default);

    /// <summary>Every still-open alert raised for a product — closed when the product is deactivated.</summary>
    Task<IEnumerable<Alert>> FindActiveByProductIdAsync(int productId, CancellationToken cancellationToken = default);
}
