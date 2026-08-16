using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Backs the Dashboard "STOCK_MOVEMENTS" report — all four filters are optional and combinable.</summary>
    Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        int? productId, int? supplierId, CancellationToken cancellationToken = default);
}
