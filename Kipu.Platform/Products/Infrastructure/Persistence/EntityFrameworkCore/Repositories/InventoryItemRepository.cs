using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

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
