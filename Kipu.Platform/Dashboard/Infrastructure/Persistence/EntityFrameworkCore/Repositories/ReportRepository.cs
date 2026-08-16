using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Dashboard.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    public async Task<IEnumerable<Report>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Report>().Where(report => report.BusinessId == businessId)
            .OrderByDescending(report => report.GeneratedAt).ToListAsync(cancellationToken);
    }
}
