using System.Security.Claims;
using Bodega.Platform.Iam.Domain.Model.Aggregates;

namespace Bodega.Platform.Iam.Application.Internal.OutboundServices;

public interface ITokenService
{
    string GenerateToken(User user);

    /// <summary>
    ///     Validates a bearer token and, if valid, returns the ClaimsPrincipal
    ///     built from its claims (attached to HttpContext.User by
    ///     RequestAuthorizationMiddleware) — or null if the token is missing,
    ///     expired, or its signature doesn't match.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
