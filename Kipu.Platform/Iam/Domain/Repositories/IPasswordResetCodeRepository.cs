using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Iam.Domain.Repositories;

public interface IPasswordResetCodeRepository : IBaseRepository<PasswordResetCode>
{
    /// <summary>The most recent outstanding code for this user, if any — there's only ever one at a time (see PasswordResetCode).</summary>
    Task<PasswordResetCode?> FindLatestByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Clears out any code(s) already on file for this user before a new one is issued.</summary>
    Task RemoveAllForUserAsync(int userId, CancellationToken cancellationToken = default);
}
