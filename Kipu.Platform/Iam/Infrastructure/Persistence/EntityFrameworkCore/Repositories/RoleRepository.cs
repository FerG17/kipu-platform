using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class RoleRepository(AppDbContext context) : BaseRepository<Role>(context), IRoleRepository
{
}
