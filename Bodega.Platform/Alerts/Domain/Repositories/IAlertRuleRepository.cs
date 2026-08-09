using Bodega.Platform.Alerts.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Domain.Repositories;

public interface IAlertRuleRepository : IBaseRepository<AlertRule>
{
    Task<IEnumerable<AlertRule>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<AlertRule?> FindByBusinessIdAndTypeAsync(int businessId, string alertType, CancellationToken cancellationToken = default);
}
