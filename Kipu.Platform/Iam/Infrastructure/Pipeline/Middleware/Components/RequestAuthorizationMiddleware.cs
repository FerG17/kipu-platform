using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
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
///
///     Finally, guards against CSRF on the cookie path: the session cookie is
///     SameSite=Strict in production (frontend and API now share a site via
///     api.kipuapp.co.uk), but this check stays as defense in depth from when
///     the cookie was SameSite=None (API on Railway's own subdomain, a
///     different site from the Cloudflare Pages frontend by the Public
///     Suffix List, so Strict/Lax would never have been sent). The header
///     path doesn't need this — a script on another site has no way to read
///     this API's cookie into an Authorization header, so there's nothing
///     ambient for it to ride on.
/// </summary>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS" };

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService, IUserRepository userRepository,
        IConfiguration configuration)
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
        var cookieToken = context.Request.Cookies["bodega_session"];
        var token = cookieToken ?? context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').LastOrDefault();
        var principal = token != null ? tokenService.ValidateToken(token) : null;

        if (principal == null || !await IsSessionStillValidAsync(principal, userRepository, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (cookieToken != null && !SafeMethods.Contains(context.Request.Method)
                                 && !HasTrustedOrigin(context.Request, configuration))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
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

    /// <summary>
    ///     True only when the request names one of this deployment's own
    ///     frontend origins (same list CORS already trusts) as its source.
    ///     Checks Origin first, falling back to Referer's origin — real
    ///     browsers always send at least one of these on a fetch/XHR/form
    ///     submission, so a cookie-authenticated request with neither is
    ///     treated as untrusted rather than let through.
    /// </summary>
    private static bool HasTrustedOrigin(HttpRequest request, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0) return false;

        var origin = FirstNonEmpty(request.Headers.Origin);
        if (origin != null)
            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

        var referer = FirstNonEmpty(request.Headers.Referer);
        if (referer != null && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            return allowedOrigins.Contains(refererUri.GetLeftPart(UriPartial.Authority), StringComparer.OrdinalIgnoreCase);

        return false;
    }

    private static string? FirstNonEmpty(StringValues values) =>
        values.FirstOrDefault(value => !string.IsNullOrEmpty(value));
}
