using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IInventoryItemRepository : IBaseRepository<InventoryItem>
{
    Task<InventoryItem?> FindByProductAndWarehouseAsync(int productId, int warehouseId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<InventoryItem>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<IEnumerable<InventoryItem>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
