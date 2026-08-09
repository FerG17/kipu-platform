using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Products.Domain.Model.Errors;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

/// <summary>
///     Translates a Result/Result&lt;T&gt; into an IActionResult, mapping
///     ProductError to the right HTTP status code — keeps this mapping out
///     of every controller.
/// </summary>
public static class ProductActionResultAssembler
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
        var statusCode = error is ProductError productError
            ? MapErrorToStatusCode(productError)
            : StatusCodes.Status500InternalServerError;
        return problemDetailsFactory.ToActionResult(statusCode, error?.ToString() ?? "InternalServerError", message);
    }

    private static int MapErrorToStatusCode(ProductError error)
    {
        return error switch
        {
            ProductError.ProductNotFound => StatusCodes.Status404NotFound,
            ProductError.WarehouseNotFound => StatusCodes.Status404NotFound,
            ProductError.InventoryItemNotFound => StatusCodes.Status404NotFound,
            ProductError.CannotDeleteWithStock => StatusCodes.Status409Conflict,
            ProductError.InsufficientStock => StatusCodes.Status409Conflict,
            ProductError.InvalidQuantity => StatusCodes.Status400BadRequest,
            ProductError.WarehouseRequired => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
