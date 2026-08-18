using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Infrastructure.Bootstrap;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;
using Kipu.Platform.Iam.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Iam.Interfaces.Rest;

[ApiController]
[Route("api/v1/authentication")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Sign-in and sign-up")]
[EnableRateLimiting("auth")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    ICurrentUserAccessor currentUserAccessor,
    IOptions<TokenSettings> tokenSettings,
    IOptions<BootstrapSettings> bootstrapSettings,
    IWebHostEnvironment environment,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    private const string SessionCookieName = "bodega_session";
    private const string BootstrapKeyHeader = "X-Bootstrap-Key";

    /// <summary>
    ///     Also sets the token as an httpOnly cookie so the frontend never has
    ///     to store the raw JWT itself (see RequestAuthorizationMiddleware,
    ///     which accepts either this cookie or the classic Authorization
    ///     header — the header stays for Swagger/tests/API consumers, the
    ///     cookie is what the real SPA relies on).
    ///
    ///     SameSite=Strict only works when the frontend and API share a site
    ///     (same registrable domain). The chosen hosting — Railway for the API,
    ///     Cloudflare Pages for the frontend, no shared custom domain yet — puts
    ///     them on two different sites by the Public Suffix List, so a Strict
    ///     cookie would simply never be sent and login would loop forever.
    ///     None is scoped to non-Development only: the dev cookie stays Strict
    ///     (see SessionCookieTests) because frontend and API are both
    ///     http://localhost there, same-site by definition, and a None cookie
    ///     requires Secure — which plain http can't satisfy anyway. Downgrading
    ///     to None removes the browser's own CSRF defense for this cookie, so
    ///     RequestAuthorizationMiddleware compensates with an Origin/Referer
    ///     check on every cookie-authenticated state-changing request.
    /// </summary>
    private void SetSessionCookie(string token)
    {
        var isProduction = !environment.IsDevelopment();
        Response.Cookies.Append(SessionCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(tokenSettings.Value.ExpirationDays)
        });
    }

    /// <summary>Authenticates a user with email/password and returns a JWT.</summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Sign in", OperationId = "SignIn")]
    [SwaggerResponse(StatusCodes.Status200OK, "Authenticated", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials")]
    public async Task<IActionResult> SignIn([FromBody] SignInResource resource, CancellationToken cancellationToken)
    {
        var command = SignInCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            authenticated =>
            {
                SetSessionCookie(authenticated.token);
                return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(
                    authenticated.user, authenticated.token));
            });
    }

    /// <summary>
    ///     Creates a User and its Business atomically, and returns a JWT —
    ///     the frontend auto-logs the user in right after registering.
    ///
    ///     Public self-service sign-up is closed: this is now how the
    ///     platform administrator provisions a new client's account by hand
    ///     (via the bootstrap key, never through the app's UI), who then
    ///     hands the credentials to the client. The client's own team members
    ///     are created afterward through POST /api/v1/users instead, which is
    ///     always scoped to the caller's existing business.
    /// </summary>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Sign up (platform-admin only, requires X-Bootstrap-Key)", OperationId = "SignUp")]
    [SwaggerResponse(StatusCodes.Status200OK, "Account created", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Missing or invalid bootstrap key")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already registered")]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource, CancellationToken cancellationToken)
    {
        if (!HasValidBootstrapKey())
            return problemDetailsFactory.ToActionResult(StatusCodes.Status403Forbidden, "SignUpForbidden",
                "Public sign-up is closed. Contact the platform administrator for an account.");

        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            authenticated =>
            {
                SetSessionCookie(authenticated.token);
                return Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(
                    authenticated.user, authenticated.token));
            });
    }

    /// <summary>
    ///     Ends the current session: clears the cookie and bumps TokenVersion
    ///     so the token that was just in use — cookie or header, this one or
    ///     any other still floating around — stops working immediately
    ///     instead of staying valid until it naturally expires.
    /// </summary>
    [HttpPost("sign-out")]
    [Authorize]
    [SwaggerOperation(Summary = "Sign out", OperationId = "SignOut")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Session ended")]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        if (currentUserAccessor.CurrentUserId == null) return Unauthorized();

        var result = await userCommandService.Handle(new SignOutCommand(currentUserAccessor.CurrentUserId.Value), cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () =>
        {
            Response.Cookies.Delete(SessionCookieName, new CookieOptions { Path = "/" });
            return NoContent();
        });
    }

    /// <summary>
    ///     Always 200, whether or not the email belongs to a real account —
    ///     answering differently would let this endpoint be used to test
    ///     which emails are registered.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Request a password-reset code by email", OperationId = "ForgotPassword")]
    [SwaggerResponse(StatusCodes.Status200OK, "A code was sent, if that email belongs to an active account")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordResource resource, CancellationToken cancellationToken)
    {
        var command = RequestPasswordResetCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => Ok());
    }

    /// <summary>Checks a code without resetting anything — lets the UI confirm it before showing the new-password step.</summary>
    [HttpPost("verify-reset-code")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Verify a password-reset code", OperationId = "VerifyResetCode")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid or expired code")]
    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeResource resource, CancellationToken cancellationToken)
    {
        var command = VerifyPasswordResetCodeCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => Ok());
    }

    /// <summary>Sets a new password against a code already confirmed via VerifyResetCode — also signs the user out of every other session.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Reset a password using a verified code", OperationId = "ResetPassword")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid/expired/unverified code, or a weak new password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordResource resource, CancellationToken cancellationToken)
    {
        var command = ResetPasswordCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }

    /// <summary>
    ///     Constant-time comparison — this header is a bearer secret, so a
    ///     length-dependent early-exit compare would let an attacker recover
    ///     it byte by byte via response timing, the same class of bug the JWT
    ///     signing key takes seriously elsewhere in this codebase.
    /// </summary>
    private bool HasValidBootstrapKey()
    {
        var provided = Request.Headers[BootstrapKeyHeader].ToString();
        var expected = bootstrapSettings.Value.Key;
        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(expected)) return false;

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
