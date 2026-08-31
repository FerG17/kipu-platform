using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Date filters are the bodega's calendar days, converted to UTC instants
///     through IBusinessClock. They used to be treated as UTC midnight-to-
///     midnight, so "sales from today" silently excluded everything sold
///     after 19:00 local and swept in the previous evening's takings instead —
///     the report never matched the till.
/// </summary>
public class SaleRepository(AppDbContext context, IBusinessClock businessClock)
    : BaseRepository<Sale>(context), ISaleRepository
{
    public async Task<IEnumerable<Sale>> FindAllByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Sale>().Include(sale => sale.SaleDetails).Where(sale => sale.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(sale => sale.Date >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(sale => sale.Date <= businessClock.EndOfDay(dateTo.Value));

        return await query.OrderByDescending(sale => sale.Date).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Sale>> FindPageByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Sale>().Where(sale => sale.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(sale => sale.Date >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(sale => sale.Date <= businessClock.EndOfDay(dateTo.Value));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Include(sale => sale.SaleDetails).OrderByDescending(sale => sale.Date)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Sale>(items, totalCount, page.Page, page.PageSize);
    }

    public async Task<Sale?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Sale>().Include(sale => sale.SaleDetails)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);
    }

    public async Task<decimal> SumPaidTotalByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Sale>().Where(sale => sale.BusinessId == businessId && sale.Status == SaleStatus.Paid);
        if (dateFrom.HasValue) query = query.Where(sale => sale.Date >= businessClock.StartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(sale => sale.Date <= businessClock.EndOfDay(dateTo.Value));

        return await query.SumAsync(sale => sale.TotalAmount, cancellationToken);
    }

    public async Task<Sale?> FindByBusinessIdAndIdempotencyKeyAsync(int businessId, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Sale>().Include(sale => sale.SaleDetails)
            .FirstOrDefaultAsync(sale => sale.BusinessId == businessId && sale.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — see ISaleRepository.</summary>
    public async Task<IEnumerable<Sale>> FindAllIgnoringTenantByIdsAsync(IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];

        return await Context.Set<Sale>().IgnoreQueryFilters()
            .Where(sale => idList.Contains(sale.Id)).ToListAsync(cancellationToken);
    }
}
