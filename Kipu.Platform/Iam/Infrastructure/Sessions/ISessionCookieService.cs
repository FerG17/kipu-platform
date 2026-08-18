using Microsoft.AspNetCore.Http;

namespace Kipu.Platform.Iam.Infrastructure.Sessions;

/// <summary>
///     Sets/clears the httpOnly session cookie — pulled out of
///     AuthenticationController so any other controller that issues a fresh
///     token after mutating the caller's own session (e.g.
///     UsersController.ChangePassword, which bumps TokenVersion and would
///     otherwise leave the caller's existing cookie instantly stale) uses
///     the exact same attributes instead of a second, driftable copy.
/// </summary>
public interface ISessionCookieService
{
    void SetSessionCookie(HttpResponse response, string token);
    void ClearSessionCookie(HttpResponse response);
}
