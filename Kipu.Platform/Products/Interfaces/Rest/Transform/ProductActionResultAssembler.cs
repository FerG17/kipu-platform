using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Products.Domain.Model.Errors;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

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
            ProductError.InvalidExpirationDate => StatusCodes.Status400BadRequest,
            ProductError.InvalidPurchasePrice => StatusCodes.Status400BadRequest,
            ProductError.BatchNotFound => StatusCodes.Status404NotFound,
            ProductError.BatchAlreadyDiscarded => StatusCodes.Status409Conflict,
            ProductError.BatchNotEditable => StatusCodes.Status409Conflict,
            ProductError.ProductInactive => StatusCodes.Status409Conflict,
            ProductError.InvalidAdjustmentQuantity => StatusCodes.Status400BadRequest,
            ProductError.AdjustmentReasonRequired => StatusCodes.Status400BadRequest,
            ProductError.AdjustmentExceedsAvailableStock => StatusCodes.Status409Conflict,
            ProductError.InvalidProductData => StatusCodes.Status400BadRequest,
            ProductError.DuplicateBarcode => StatusCodes.Status409Conflict,
            ProductError.SupplierNotFound => StatusCodes.Status400BadRequest,
            ProductError.InvalidWarehouseData => StatusCodes.Status400BadRequest,
            ProductError.ConcurrentModification => StatusCodes.Status409Conflict,
            ProductError.InvalidAdjustmentReason => StatusCodes.Status400BadRequest,
            ProductError.InvalidStockIntakeData => StatusCodes.Status400BadRequest,
            ProductError.WarehouseInactive => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
