using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().Where(user => user.BusinessId == businessId).ToListAsync(cancellationToken);
    }
}
