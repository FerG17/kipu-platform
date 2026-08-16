using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class StockMovementRepository(AppDbContext context, IBusinessClock businessClock)
    : BaseRepository<StockMovement>(context), IStockMovementRepository
{
    public async Task<IEnumerable<StockMovement>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<StockMovement>().Where(movement => movement.BusinessId == businessId)
            .OrderByDescending(movement => movement.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom,
        DateOnly? dateTo, int? productId, int? supplierId, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<StockMovement>().Where(movement => movement.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(movement => movement.RegisteredAt >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(movement => movement.RegisteredAt <= businessClock.EndOfDay(dateTo.Value));
        if (productId.HasValue) query = query.Where(movement => movement.ProductId == productId.Value);

        // By id, not by the supplier's name: the name is a snapshot taken
        // when the goods arrived, so matching on it dropped every movement
        // from a supplier that had since been renamed.
        if (supplierId.HasValue) query = query.Where(movement => movement.SupplierId == supplierId.Value);

        return await query.OrderByDescending(movement => movement.RegisteredAt).ToListAsync(cancellationToken);
    }
}
