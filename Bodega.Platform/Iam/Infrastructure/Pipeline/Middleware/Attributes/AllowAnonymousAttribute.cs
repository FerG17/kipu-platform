namespace Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Marks an action (or controller) as reachable without a valid bearer
///     token — checked by RequestAuthorizationMiddleware, not ASP.NET Core's
///     built-in authorization pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AllowAnonymousAttribute : Attribute
{
}
