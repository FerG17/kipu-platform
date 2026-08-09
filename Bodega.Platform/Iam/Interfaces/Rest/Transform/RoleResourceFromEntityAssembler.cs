using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class RoleResourceFromEntityAssembler
{
    public static RoleResource ToResourceFromEntity(Role role)
    {
        return new RoleResource(role.Id, role.Position);
    }
}
