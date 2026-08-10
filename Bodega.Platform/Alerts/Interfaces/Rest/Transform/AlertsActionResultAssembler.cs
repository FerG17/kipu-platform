using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Alerts.Domain.Model.Errors;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Bodega.Platform.Alerts.Interfaces.Rest.Transform;

public static class AlertsActionResultAssembler
{
    public static IActionResult ToActionResult<T>(
        Result<T> result,
        ProblemDetailsFactory problemDetailsFactory,
        Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess(result.Value!)
            : ToProblemResult(result.Error, result.Message, problemDetailsFactory);
    }

    private static IActionResult ToProblemResult(Enum? error, string message, ProblemDetailsFactory problemDetailsFactory)
    {
        var statusCode = error is AlertsError alertsError
            ? MapErrorToStatusCode(alertsError)
            : StatusCodes.Status500InternalServerError;
        return problemDetailsFactory.ToActionResult(statusCode, error?.ToString() ?? "InternalServerError", message);
    }

    private static int MapErrorToStatusCode(AlertsError error)
    {
        return error switch
        {
            AlertsError.AlertNotFound => StatusCodes.Status404NotFound,
            AlertsError.AlertAlreadyResolved => StatusCodes.Status409Conflict,
            AlertsError.InvalidThreshold => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
