using Microsoft.AspNetCore.Mvc;

namespace Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;

/// <summary>
///     Builds RFC 7807 ProblemDetails responses consistently across every
///     bounded context, so API error shapes never drift between controllers.
/// </summary>
public class ProblemDetailsFactory
{
    public Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string? instance = null)
    {
        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = instance,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
    }

    public ActionResult ToActionResult(int statusCode, string title, string detail, string? instance = null)
    {
        var problemDetails = CreateProblemDetails(statusCode, title, detail, instance);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
