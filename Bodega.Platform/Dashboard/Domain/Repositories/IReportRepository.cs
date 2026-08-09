using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Dashboard.Domain.Repositories;

public interface IReportRepository : IBaseRepository<Report>
{
    Task<IEnumerable<Report>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
