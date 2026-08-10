using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AlertRuleRepository(AppDbContext context) : BaseRepository<AlertRule>(context), IAlertRuleRepository
{
    public async Task<IEnumerable<AlertRule>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<AlertRule>().Where(rule => rule.BusinessId == businessId).ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     IgnoreQueryFilters() deliberately: businessId is an explicit
    ///     parameter already applied in the Where clause below, which is the
    ///     real scoping here — that's what lets this stay correct both from
    ///     request-scoped callers (businessId == CurrentBusinessId anyway) and
    ///     from the alerts expiration sweep, which iterates every business
    ///     outside any authenticated context.
    /// </summary>
    public async Task<AlertRule?> FindByBusinessIdAndTypeAsync(int businessId, string alertType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AlertRule>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(rule => rule.BusinessId == businessId && rule.AlertType == alertType, cancellationToken);
    }
}
