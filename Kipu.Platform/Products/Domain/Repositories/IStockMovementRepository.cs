using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<PagedResult<StockMovement>> FindAllByBusinessIdAsync(int businessId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Backs the Dashboard "STOCK_MOVEMENTS" report and the Kardex view — every filter is
    ///     optional and combinable. Always includes each movement's Batch (for Kardex's unit-cost
    ///     column, sourced from Batch.PurchasePrice); category filters by the owning Product's
    ///     Category. `ascending` defaults to false to preserve the report's existing newest-first
    ///     order — Kardex passes true, since a running balance only makes sense oldest-first.
    /// </summary>
    Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        int? productId, int? supplierId, string? category = null, bool ascending = false, CancellationToken cancellationToken = default);
}
