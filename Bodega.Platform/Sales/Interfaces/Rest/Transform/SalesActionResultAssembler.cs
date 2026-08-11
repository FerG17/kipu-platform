using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Sales.Domain.Model.Errors;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class SalesActionResultAssembler
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

    public static IActionResult ToActionResult(
        Result result,
        ProblemDetailsFactory problemDetailsFactory,
        Func<IActionResult> onSuccess)
    {
        return result.IsSuccess
            ? onSuccess()
            : ToProblemResult(result.Error, result.Message, problemDetailsFactory);
    }

    private static IActionResult ToProblemResult(Enum? error, string message, ProblemDetailsFactory problemDetailsFactory)
    {
        var statusCode = error is SalesError salesError
            ? MapErrorToStatusCode(salesError)
            : StatusCodes.Status500InternalServerError;
        return problemDetailsFactory.ToActionResult(statusCode, error?.ToString() ?? "InternalServerError", message);
    }

    private static int MapErrorToStatusCode(SalesError error)
    {
        return error switch
        {
            SalesError.SaleNotFound => StatusCodes.Status404NotFound,
            SalesError.CustomerNotFound => StatusCodes.Status404NotFound,
            SalesError.ProductNotFound => StatusCodes.Status404NotFound,
            SalesError.InsufficientStock => StatusCodes.Status409Conflict,
            SalesError.SaleAlreadyCancelled => StatusCodes.Status409Conflict,
            SalesError.EmptySaleLines => StatusCodes.Status400BadRequest,
            SalesError.InvalidSaleLine => StatusCodes.Status400BadRequest,
            SalesError.InvalidStatusTransition => StatusCodes.Status400BadRequest,
            SalesError.PaymentPlanNotFound => StatusCodes.Status404NotFound,
            SalesError.PaymentPlanAlreadyExists => StatusCodes.Status409Conflict,
            SalesError.InstallmentsFullyPaid => StatusCodes.Status409Conflict,
            SalesError.InvalidInstallmentCount => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
