using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    /// <summary>
    ///     IgnoreQueryFilters() deliberately: email is unique across the whole
    ///     table (not per business — see ix_users_email), and sign-in needs to
    ///     find the user before a tenant is known at all. The BusinessId query
    ///     filter would otherwise fail closed here (no authenticated business
    ///     yet) and break every login.
    /// </summary>
    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    /// <summary>Same reasoning as FindByEmailAsync — used to enforce the global email-uniqueness rule during sign-up/invite.</summary>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().IgnoreQueryFilters().AnyAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().Where(user => user.BusinessId == businessId).ToListAsync(cancellationToken);
    }

    public async Task<User?> FindByIdIgnoringTenantAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<User>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }
}
