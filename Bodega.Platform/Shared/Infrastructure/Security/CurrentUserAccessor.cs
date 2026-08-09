using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Bodega.Platform.Shared.Application;

namespace Bodega.Platform.Shared.Infrastructure.Security;

/// <summary>
///     Reads the current user/business/role from HttpContext.User claims set
///     by the IAM authorization middleware once a bearer token is validated.
///     Claim types are the contract with IAM's TokenService — kept as plain
///     strings here rather than referencing IAM directly, so Shared never
///     depends on a specific bounded context.
/// </summary>
public class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    private const string BusinessIdClaimType = "business_id";

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? CurrentUserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public int? CurrentBusinessId
    {
        get
        {
            var value = User?.FindFirst(BusinessIdClaimType)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? CurrentUserRole => User?.FindFirst(ClaimTypes.Role)?.Value;
}
