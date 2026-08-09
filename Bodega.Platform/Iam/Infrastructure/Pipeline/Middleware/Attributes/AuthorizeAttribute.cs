namespace Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Documents that an action requires a valid bearer token. Purely
///     informational today — RequestAuthorizationMiddleware requires a valid
///     token on every endpoint by default, unless [AllowAnonymous] is present.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
}
