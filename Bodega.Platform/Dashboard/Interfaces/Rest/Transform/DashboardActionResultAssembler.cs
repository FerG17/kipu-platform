using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Dashboard.Domain.Model.Errors;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Bodega.Platform.Dashboard.Interfaces.Rest.Transform;

public static class DashboardActionResultAssembler
{
    public static IActionResult ToActionResult<T>(
        Result<T> result,
        ProblemDetailsFactory problemDetailsFactory,
        Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess) return onSuccess(result.Value!);

        var statusCode = result.Error switch
        {
            DashboardError.ReportNotFound => StatusCodes.Status404NotFound,
            DashboardError.UnsupportedReportTypeForPdf => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        return problemDetailsFactory.ToActionResult(statusCode, result.Error?.ToString() ?? "InternalServerError", result.Message);
    }
}
