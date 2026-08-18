using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Errors;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

/// <summary>
///     Translates a Result/Result&lt;T&gt; into an IActionResult, mapping
///     IamError to the right HTTP status code and building a ProblemDetails
///     response on failure — keeps this mapping out of every controller.
/// </summary>
public static class IamActionResultAssembler
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
        var statusCode = error is IamError iamError ? MapErrorToStatusCode(iamError) : StatusCodes.Status500InternalServerError;
        return problemDetailsFactory.ToActionResult(statusCode, error?.ToString() ?? "InternalServerError", message);
    }

    private static int MapErrorToStatusCode(IamError error)
    {
        return error switch
        {
            IamError.InvalidCredentials => StatusCodes.Status401Unauthorized,
            IamError.CurrentPasswordInvalid => StatusCodes.Status401Unauthorized,
            IamError.WeakPassword => StatusCodes.Status400BadRequest,
            IamError.RoleNotFound => StatusCodes.Status400BadRequest,
            IamError.InvalidResetCode => StatusCodes.Status400BadRequest,
            IamError.EmailAlreadyTaken => StatusCodes.Status409Conflict,
            IamError.CannotRemoveLastAdmin => StatusCodes.Status409Conflict,
            IamError.CannotRemoveOwnAccess => StatusCodes.Status409Conflict,
            IamError.ConcurrentModification => StatusCodes.Status409Conflict,
            IamError.UserNotFound => StatusCodes.Status404NotFound,
            IamError.BusinessNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
