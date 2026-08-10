using System.Security.Claims;
using Bodega.Platform.Iam.Domain.Model.Aggregates;

namespace Bodega.Platform.Iam.Application.Internal.OutboundServices;

public interface ITokenService
{
    /// <summary>roleName is the Role's Position (e.g. "ADMIN") — embedded as the token's role claim, checked by [Authorize(Roles = ...)].</summary>
    string GenerateToken(User user, string roleName);

    /// <summary>
    ///     Validates a bearer token and, if valid, returns the ClaimsPrincipal
    ///     built from its claims (attached to HttpContext.User by
    ///     RequestAuthorizationMiddleware) — or null if the token is missing,
    ///     expired, or its signature doesn't match.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
