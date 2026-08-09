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

    public async Task<AlertRule?> FindByBusinessIdAndTypeAsync(int businessId, string alertType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<AlertRule>()
            .FirstOrDefaultAsync(rule => rule.BusinessId == businessId && rule.AlertType == alertType, cancellationToken);
    }
}
