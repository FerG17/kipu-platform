using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kipu.Platform.Shared.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Catches any exception that escapes a controller/middleware further down
///     the pipeline (including the IAM authorization middleware once it's
///     wired up) and turns it into a standard RFC 7807 ProblemDetails
///     response, so no stack trace or internal detail ever leaks to a client.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client hung up mid-request. Nothing to report to a socket
            // that is already gone, and logging it as an error buries real
            // failures under noise from every closed browser tab.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

            // Once the response is on the wire its status and headers are
            // fixed; touching them here throws a second exception that
            // replaces the original in the logs. A half-written body is all
            // the caller can get at that point.
            if (context.Response.HasStarted) throw;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please try again later or contact support if the problem persists.",
                Type = "https://httpstatuses.com/500"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
