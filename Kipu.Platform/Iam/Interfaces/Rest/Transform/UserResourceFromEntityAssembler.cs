using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResourceFromEntity(User user)
    {
        return new UserResource(user.Id, user.Email, user.Name, user.LastName, user.BusinessId, user.RoleId,
            user.Status, user.Phone);
    }
}
