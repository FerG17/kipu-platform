using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class BusinessRepository(AppDbContext context) : BaseRepository<Business>(context), IBusinessRepository
{
}
