using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResourceFromEntity(User user, string token)
    {
        return new AuthenticatedUserResource(user.Id, user.Email, user.Name, user.LastName, user.BusinessId,
            user.RoleId, user.Status, user.Phone, token);
    }
}
