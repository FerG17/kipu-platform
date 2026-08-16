using System.Security.Claims;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Services;

namespace Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Validates the bearer token on every request unless the matched
///     endpoint carries [AllowAnonymous]. On success, attaches the token's
///     claims to HttpContext.User so Shared's ICurrentUserAccessor can
///     resolve the current user/business/role anywhere in the application
///     layer without any code touching HttpContext directly.
///
///     Beyond signature/expiry (checked by ITokenService.ValidateToken), this
///     also re-checks the user's current Status and TokenVersion against the
///     database on every request — this is what makes a password change,
///     an explicit "log out everywhere", or deactivating a user actually
///     revoke already-issued tokens immediately, instead of leaving them
///     valid until they naturally expire.
///
///     Also enforces role-based authorization: if the matched endpoint's
///     [Authorize] attribute lists specific roles, the token's role claim
///     must be one of them, or the request is rejected with 403 — see
///     AuthorizeAttribute for the full role matrix rationale.
/// </summary>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IUserRepository userRepository)
    {
        var allowAnonymous = context.GetEndpoint()?.Metadata
            .Any(metadata => metadata is AllowAnonymousAttribute) ?? false;

        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        // The cookie is what the real SPA relies on (see AuthenticationController,
        // which sets it httpOnly on sign-in/sign-up) — the header stays accepted
        // as a fallback for Swagger, the integration test suite, and any future
        // non-browser API consumer that would rather manage a token itself.
        var token = context.Request.Cookies["bodega_session"]
                    ?? context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').LastOrDefault();
        var principal = token != null ? tokenService.ValidateToken(token) : null;

        if (principal == null || !await IsSessionStillValidAsync(principal, userRepository, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var authorizeAttribute = context.GetEndpoint()?.Metadata.GetMetadata<AuthorizeAttribute>();
        if (authorizeAttribute is { Roles.Length: > 0 })
        {
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (role == null || !authorizeAttribute.Roles.Contains(role))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        context.User = principal;

        await next(context);
    }

    private static async Task<bool> IsSessionStillValidAsync(ClaimsPrincipal principal, IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenVersionClaim = principal.FindFirst(TokenService.TokenVersionClaimType)?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || !int.TryParse(tokenVersionClaim, out var tokenVersion))
            return false;

        var user = await userRepository.FindByIdIgnoringTenantAsync(userId, cancellationToken);
        return user is { Status: UserStatus.Active } && user.TokenVersion == tokenVersion;
    }
}
