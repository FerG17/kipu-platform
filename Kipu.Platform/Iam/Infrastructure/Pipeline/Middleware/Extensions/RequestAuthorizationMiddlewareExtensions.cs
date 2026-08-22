using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

namespace Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;

public static class RequestAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestAuthorizationMiddleware>();
    }
}
