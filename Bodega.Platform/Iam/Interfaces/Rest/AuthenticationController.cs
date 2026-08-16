using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Infrastructure.Bootstrap;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;
using Bodega.Platform.Iam.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Iam.Interfaces.Rest;

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
    /// </summary>
    private void SetSessionCookie(string token)
    {
        Response.Cookies.Append(SessionCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
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
