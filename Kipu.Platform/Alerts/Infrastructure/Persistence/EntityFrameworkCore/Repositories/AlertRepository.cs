using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AlertRepository(AppDbContext context) : BaseRepository<Alert>(context), IAlertRepository
{
    public async Task<IEnumerable<Alert>> FindActiveByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>()
            .Where(alert => alert.BusinessId == businessId && alert.Status != AlertStatus.Resolved)
            .OrderByDescending(alert => alert.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Alert>> FindResolvedByBusinessIdAsync(int businessId, PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Alert>().Where(alert => alert.BusinessId == businessId && alert.Status == AlertStatus.Resolved);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(alert => alert.ResolvedAt)
            .Skip(page.Skip).Take(page.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Alert>(items, totalCount, page.Page, page.PageSize);
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

    /// <summary>Tenant-filtered, unlike the upsert lookup above — this only ever runs inside an authenticated request.</summary>
    public async Task<IEnumerable<Alert>> FindActiveByBatchIdAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>()
            .Where(alert => alert.BatchId == batchId && alert.Status != AlertStatus.Resolved)
            .ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — same reasoning as FindActiveByProductAndTypeAsync.</summary>
    public async Task<IEnumerable<Alert>> FindActiveExpirationAlertsByBatchIdsAsync(IReadOnlyCollection<int> batchIds,
        CancellationToken cancellationToken = default)
    {
        if (batchIds.Count == 0) return [];

        return await Context.Set<Alert>().IgnoreQueryFilters()
            .Where(alert => alert.BatchId != null && batchIds.Contains(alert.BatchId.Value)
                             && (alert.Type == AlertType.Expiration || alert.Type == AlertType.Expired)
                             && alert.Status != AlertStatus.Resolved)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Tenant-filtered, same reasoning as FindActiveByBatchIdAsync.</summary>
    public async Task<IEnumerable<Alert>> FindActiveByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Alert>()
            .Where(alert => alert.ProductId == productId && alert.Status != AlertStatus.Resolved)
            .ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — same reasoning as FindActiveExpirationAlertsByBatchIdsAsync.</summary>
    public async Task<IEnumerable<Alert>> FindActiveInstallmentAlertsBySaleIdsAsync(IReadOnlyCollection<int> saleIds,
        CancellationToken cancellationToken = default)
    {
        if (saleIds.Count == 0) return [];

        return await Context.Set<Alert>().IgnoreQueryFilters()
            .Where(alert => alert.SaleId != null && saleIds.Contains(alert.SaleId.Value)
                             && alert.Type == AlertType.InstallmentDue && alert.Status != AlertStatus.Resolved)
            .ToListAsync(cancellationToken);
    }
}
