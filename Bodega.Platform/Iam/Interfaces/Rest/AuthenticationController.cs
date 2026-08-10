using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;
using Bodega.Platform.Iam.Interfaces.Rest.Transform;
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
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
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
            authenticated => Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(
                authenticated.user, authenticated.token)));
    }

    /// <summary>
    ///     Creates a User and its Business atomically, and returns a JWT —
    ///     the frontend auto-logs the user in right after registering.
    /// </summary>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Sign up", OperationId = "SignUp")]
    [SwaggerResponse(StatusCodes.Status200OK, "Account created", typeof(AuthenticatedUserResource))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already registered")]
    public async Task<IActionResult> SignUp([FromBody] SignUpResource resource, CancellationToken cancellationToken)
    {
        var command = SignUpCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            authenticated => Ok(AuthenticatedUserResourceFromEntityAssembler.ToResourceFromEntity(
                authenticated.user, authenticated.token)));
    }
}
