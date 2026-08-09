using Microsoft.AspNetCore.Builder;
using Bodega.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;

namespace Bodega.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    ///     Registers the global exception handler as early as possible in the
    ///     pipeline, so it wraps every other middleware (including the IAM
    ///     authorization middleware added in Phase 1).
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
