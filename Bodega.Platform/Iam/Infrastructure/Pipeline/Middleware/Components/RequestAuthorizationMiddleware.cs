using Bodega.Platform.Iam.Application.Internal.OutboundServices;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

namespace Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Validates the bearer token on every request unless the matched
///     endpoint carries [AllowAnonymous]. On success, attaches the token's
///     claims to HttpContext.User so Shared's ICurrentUserAccessor can
///     resolve the current user/business/role anywhere in the application
///     layer without any code touching HttpContext directly.
/// </summary>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var allowAnonymous = context.GetEndpoint()?.Metadata
            .Any(metadata => metadata is AllowAnonymousAttribute) ?? false;

        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').LastOrDefault();
        var principal = token != null ? tokenService.ValidateToken(token) : null;

        if (principal == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.User = principal;

        await next(context);
    }
}
