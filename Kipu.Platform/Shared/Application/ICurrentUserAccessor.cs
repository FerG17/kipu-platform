namespace Kipu.Platform.Shared.Application;

/// <summary>
///     Resolves the identity of the currently authenticated request (user,
///     business/tenant, role) from the validated JWT, without any application
///     or command-service code needing to touch HttpContext directly.
///
///     Populated once the IAM authorization middleware (Phase 1) validates the
///     bearer token and sets the corresponding claims on HttpContext.User.
///     Null on anonymous endpoints (sign-in/sign-up) or when no valid token
///     was presented.
/// </summary>
public interface ICurrentUserAccessor
{
    int? CurrentUserId { get; }
    int? CurrentBusinessId { get; }
    string? CurrentUserRole { get; }
}
