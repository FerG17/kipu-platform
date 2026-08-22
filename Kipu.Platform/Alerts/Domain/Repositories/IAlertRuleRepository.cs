using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Domain.Repositories;

public interface IAlertRuleRepository : IBaseRepository<AlertRule>
{
    Task<IEnumerable<AlertRule>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<AlertRule?> FindByBusinessIdAndTypeAsync(int businessId, string alertType, CancellationToken cancellationToken = default);
}
