using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

namespace Kipu.Platform.Iam.Infrastructure.Sessions;

/// <summary>
///     SameSite=Strict requires the frontend and API to share a site (same
///     registrable domain) — production does, via api.kipuapp.co.uk serving
///     the API alongside kipuapp.co.uk's frontend, so the cookie is always
///     Strict regardless of environment. This used to be None in production
///     (API on Railway's own subdomain, frontend on Cloudflare Pages — two
///     different sites by the Public Suffix List, so Strict would never have
///     been sent), which is why RequestAuthorizationMiddleware still layers
///     an Origin/Referer check on top of SameSite for cookie-authenticated
///     state-changing requests — kept as defense in depth even now that
///     SameSite alone would block a genuine cross-site forgery.
/// </summary>
public class SessionCookieService(IOptions<TokenSettings> tokenSettings, IWebHostEnvironment environment)
    : ISessionCookieService
{
    public const string CookieName = "bodega_session";

    public void SetSessionCookie(HttpResponse response, string token)
    {
        var isProduction = !environment.IsDevelopment();
        response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(tokenSettings.Value.ExpirationDays)
        });
    }

    public void ClearSessionCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }
}
