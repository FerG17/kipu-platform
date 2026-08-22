using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

namespace Kipu.Platform.Iam.Infrastructure.Sessions;

/// <summary>
///     SameSite=Strict only works when the frontend and API share a site
///     (same registrable domain). The chosen hosting — Railway for the API,
///     Cloudflare Pages for the frontend, no shared custom domain yet — puts
///     them on two different sites by the Public Suffix List, so a Strict
///     cookie would simply never be sent and login would loop forever.
///     None is scoped to non-Development only: the dev cookie stays Strict
///     because frontend and API are both http://localhost there, same-site
///     by definition, and a None cookie requires Secure — which plain http
///     can't satisfy anyway. Downgrading to None removes the browser's own
///     CSRF defense for this cookie, so RequestAuthorizationMiddleware
///     compensates with an Origin/Referer check on every cookie-authenticated
///     state-changing request.
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
            SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(tokenSettings.Value.ExpirationDays)
        });
    }

    public void ClearSessionCookie(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }
}
