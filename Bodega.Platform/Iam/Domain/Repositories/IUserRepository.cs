using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Iam.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     For RequestAuthorizationMiddleware's per-request session check only
    ///     — it runs before context.User carries the business_id claim (that's
    ///     the whole point of this call: deciding whether the token is still
    ///     valid before establishing the request's tenant context), so the
    ///     regular FindByIdAsync's BusinessId filter would fail closed here.
    ///     The userId itself is only trusted because it came from an
    ///     already-signature-validated JWT.
    /// </summary>
    Task<User?> FindByIdIgnoringTenantAsync(int id, CancellationToken cancellationToken = default);
}
