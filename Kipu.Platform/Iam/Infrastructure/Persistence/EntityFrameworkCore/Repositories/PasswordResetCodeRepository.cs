using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class PasswordResetCodeRepository(AppDbContext context)
    : BaseRepository<PasswordResetCode>(context), IPasswordResetCodeRepository
{
    public async Task<PasswordResetCode?> FindLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PasswordResetCode>()
            .Where(code => code.UserId == userId)
            .OrderByDescending(code => code.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RemoveAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Set<PasswordResetCode>()
            .Where(code => code.UserId == userId)
            .ToListAsync(cancellationToken);

        Context.Set<PasswordResetCode>().RemoveRange(existing);
    }
}
