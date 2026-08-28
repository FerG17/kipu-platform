using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class StockMovementRepository(AppDbContext context, IBusinessClock businessClock)
    : BaseRepository<StockMovement>(context), IStockMovementRepository
{
    public async Task<PagedResult<StockMovement>> FindAllByBusinessIdAsync(int businessId, PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<StockMovement>().Where(movement => movement.BusinessId == businessId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Include(movement => movement.Batch).OrderByDescending(movement => movement.RegisteredAt)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<StockMovement>(items, totalCount, page.Page, page.PageSize);
    }

    public async Task<IEnumerable<StockMovement>> FindFilteredByBusinessIdAsync(int businessId, DateOnly? dateFrom,
        DateOnly? dateTo, int? productId, int? supplierId, string? category = null, bool ascending = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<StockMovement>().Include(movement => movement.Batch)
            .Where(movement => movement.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(movement => movement.RegisteredAt >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(movement => movement.RegisteredAt <= businessClock.EndOfDay(dateTo.Value));
        if (productId.HasValue) query = query.Where(movement => movement.ProductId == productId.Value);

        // By id, not by the supplier's name: the name is a snapshot taken
        // when the goods arrived, so matching on it dropped every movement
        // from a supplier that had since been renamed.
        if (supplierId.HasValue) query = query.Where(movement => movement.SupplierId == supplierId.Value);

        // Category lives on Product, not StockMovement — join rather than
        // round-trip through the product query service the way the report
        // facade resolves names, since both entities share this DbContext.
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Join(Context.Set<Product>(), movement => movement.ProductId, product => product.Id,
                (movement, product) => new { movement, product })
                .Where(joined => joined.product.Category == category)
                .Select(joined => joined.movement);
        }

        return ascending
            ? await query.OrderBy(movement => movement.RegisteredAt).ToListAsync(cancellationToken)
            : await query.OrderByDescending(movement => movement.RegisteredAt).ToListAsync(cancellationToken);
    }
}
