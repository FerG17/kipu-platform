using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class RoleRepository(AppDbContext context) : BaseRepository<Role>(context), IRoleRepository
{
}
