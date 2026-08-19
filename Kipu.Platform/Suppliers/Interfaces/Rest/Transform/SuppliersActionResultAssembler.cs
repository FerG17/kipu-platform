using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Suppliers.Domain.Model.Errors;

namespace Kipu.Platform.Suppliers.Interfaces.Rest.Transform;

public static class SuppliersActionResultAssembler
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
        var statusCode = error is SuppliersError suppliersError
            ? MapErrorToStatusCode(suppliersError)
            : StatusCodes.Status500InternalServerError;
        return problemDetailsFactory.ToActionResult(statusCode, error?.ToString() ?? "InternalServerError", message);
    }

    private static int MapErrorToStatusCode(SuppliersError error)
    {
        return error switch
        {
            SuppliersError.SupplierNotFound => StatusCodes.Status404NotFound,
            SuppliersError.PurchaseOrderNotFound => StatusCodes.Status404NotFound,
            SuppliersError.ProductNotFound => StatusCodes.Status404NotFound,
            SuppliersError.InvalidStatusTransition => StatusCodes.Status409Conflict,
            SuppliersError.EmptyPurchaseOrderLines => StatusCodes.Status400BadRequest,
            SuppliersError.InvalidPurchaseOrderLine => StatusCodes.Status400BadRequest,
            SuppliersError.InvalidSupplierData => StatusCodes.Status400BadRequest,
            SuppliersError.SupplierHasPendingOrders => StatusCodes.Status409Conflict,
            SuppliersError.ConcurrentModification => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
