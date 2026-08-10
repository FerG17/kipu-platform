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

    public async Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom,
        DateOnly? dateTo, int? productId, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<StockMovement>().Where(movement => movement.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(movement => movement.RegisteredAt >= ToStartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(movement => movement.RegisteredAt <= ToEndOfDay(dateTo.Value));
        if (productId.HasValue) query = query.Where(movement => movement.ProductId == productId.Value);

        return await query.OrderByDescending(movement => movement.RegisteredAt).ToListAsync(cancellationToken);
    }

    private static DateTimeOffset ToStartOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static DateTimeOffset ToEndOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
    }
}
