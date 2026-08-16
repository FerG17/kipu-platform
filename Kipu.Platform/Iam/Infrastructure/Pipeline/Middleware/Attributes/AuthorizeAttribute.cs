namespace Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Requires a valid bearer token on every endpoint by default (checked by
///     RequestAuthorizationMiddleware), unless [AllowAnonymous] is present.
///
///     With no roles specified, any authenticated user of any role may call
///     the endpoint — same as before roles existed. With roles specified
///     (e.g. [Authorize(RoleNames.Admin, RoleNames.Warehouse)]), the caller's
///     role (from the JWT) must be one of them, or the request is rejected
///     with 403 Forbidden. Applying [Authorize] on a controller class sets
///     the default for every action in it; an action-level [Authorize] with
///     its own roles overrides the class-level one for that single action
///     (RequestAuthorizationMiddleware checks the action's metadata first).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizeAttribute(params string[] roles) : Attribute
{
    public string[] Roles { get; } = roles;
}
