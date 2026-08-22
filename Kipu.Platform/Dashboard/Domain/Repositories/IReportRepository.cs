using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Dashboard.Domain.Repositories;

public interface IReportRepository : IBaseRepository<Report>
{
    Task<IEnumerable<Report>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
