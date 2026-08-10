using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AlertRepository(AppDbContext context) : BaseRepository<Alert>(context), IAlertRepository
{
    public async Task<IEnumerable<Alert>> FindActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>()
            .Where(alert => alert.BusinessId == businessId && alert.Status != AlertStatus.Resolved)
            .OrderByDescending(alert => alert.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Alert>> FindResolvedByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>()
            .Where(alert => alert.BusinessId == businessId && alert.Status == AlertStatus.Resolved)
            .OrderByDescending(alert => alert.ResolvedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     IgnoreQueryFilters() deliberately: called both from request-scoped
    ///     event handlers (harmless — ProductId is a globally unique PK, so
    ///     the match is exact regardless) and from the alerts expiration
    ///     sweep, which runs outside any authenticated business.
    /// </summary>
    public async Task<Alert?> FindActiveByProductAndTypeAsync(int productId, string type, int? batchId,
        int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>().IgnoreQueryFilters().FirstOrDefaultAsync(
            alert => alert.ProductId == productId && alert.Type == type && alert.BatchId == batchId
                     && alert.WarehouseId == warehouseId && alert.Status != AlertStatus.Resolved,
            cancellationToken);
    }
}
