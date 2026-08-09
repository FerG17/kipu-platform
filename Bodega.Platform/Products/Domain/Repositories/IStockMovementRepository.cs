using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
