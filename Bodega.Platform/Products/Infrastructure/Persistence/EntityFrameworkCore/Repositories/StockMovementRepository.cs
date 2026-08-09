using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class StockMovementRepository(AppDbContext context) : BaseRepository<StockMovement>(context), IStockMovementRepository
{
    public async Task<IEnumerable<StockMovement>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<StockMovement>().Where(movement => movement.BusinessId == businessId)
            .OrderByDescending(movement => movement.RegisteredAt)
            .ToListAsync(cancellationToken);
    }
}
