using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class RoleResourceFromEntityAssembler
{
    public static RoleResource ToResourceFromEntity(Role role)
    {
        return new RoleResource(role.Id, role.Position);
    }
}
