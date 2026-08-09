using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SaleRepository(AppDbContext context) : BaseRepository<Sale>(context), ISaleRepository
{
    public async Task<IEnumerable<Sale>> FindAllByBusinessIdAsync(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Sale>().Include(sale => sale.SaleDetails).Where(sale => sale.BusinessId == businessId);
        if (dateFrom.HasValue) query = query.Where(sale => sale.Date >= ToStartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(sale => sale.Date <= ToEndOfDay(dateTo.Value));

        return await query.OrderByDescending(sale => sale.Date).ToListAsync(cancellationToken);
    }

    private static DateTimeOffset ToStartOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }

    private static DateTimeOffset ToEndOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
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
        if (dateFrom.HasValue) query = query.Where(sale => sale.Date >= ToStartOfDay(dateFrom.Value));
        if (dateTo.HasValue) query = query.Where(sale => sale.Date <= ToEndOfDay(dateTo.Value));

        return await query.SumAsync(sale => sale.TotalAmount, cancellationToken);
    }
}
