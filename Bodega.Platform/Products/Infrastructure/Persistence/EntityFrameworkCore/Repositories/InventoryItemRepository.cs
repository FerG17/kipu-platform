using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class InventoryItemRepository(AppDbContext context) : BaseRepository<InventoryItem>(context), IInventoryItemRepository
{
    public async Task<InventoryItem?> FindByProductAndWarehouseAsync(int productId, int warehouseId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<InventoryItem>()
            .FirstOrDefaultAsync(item => item.ProductId == productId && item.WarehouseId == warehouseId, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> FindAllByProductIdAsync(int productId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<InventoryItem>().Where(item => item.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<InventoryItem>().Where(item => item.BusinessId == businessId)
            .ToListAsync(cancellationToken);
    }
}
