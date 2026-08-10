using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Backs the Dashboard "STOCK_MOVEMENTS" report — dateFrom/dateTo/productId are all optional and combinable.</summary>
    Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        int? productId, CancellationToken cancellationToken = default);
}
