using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Domain.Repositories;

public interface IAlertRepository : IBaseRepository<Alert>
{
    Task<IEnumerable<Alert>> FindActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Alert>> FindResolvedByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

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

    /// <summary>Every still-open alert raised for a product — closed when the product is deactivated.</summary>
    Task<IEnumerable<Alert>> FindActiveByProductIdAsync(int productId, CancellationToken cancellationToken = default);
}
