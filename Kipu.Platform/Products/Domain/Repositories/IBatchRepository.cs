using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IBatchRepository : IBaseRepository<Batch>
{
    Task<IEnumerable<Batch>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Batch>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Every ACTIVE batch for one InventoryItem (product+warehouse),
    ///     ordered earliest-expiration-first — batches with no expiration
    ///     sort last, since they carry no urgency. Includes batches with
    ///     RemainingQuantity == 0 (fully sold but not discarded); callers
    ///     filter for their own purpose — RegisterStockSaleCommand's FEFO
    ///     draw needs RemainingQuantity &gt; 0, RegisterStockReturnCommand's
    ///     restore needs spare capacity (RemainingQuantity &lt; Quantity) —
    ///     see InventoryCommandService (X5 Bloque C).
    /// </summary>
    Task<IEnumerable<Batch>> FindActiveByInventoryItemIdAsync(int inventoryItemId, CancellationToken cancellationToken = default);

    /// <summary>Every ACTIVE batch across every business — used by Alerts' expiration sweep.</summary>
    Task<IEnumerable<Batch>> FindAllActiveAsync(CancellationToken cancellationToken = default);
}
