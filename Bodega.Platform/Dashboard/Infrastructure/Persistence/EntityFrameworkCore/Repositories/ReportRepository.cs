using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    public async Task<IEnumerable<Report>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().Where(report => report.BusinessId == businessId)
            .OrderByDescending(report => report.GeneratedAt).ToListAsync(cancellationToken);
    }
}
