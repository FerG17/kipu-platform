using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class InviteUserCommandFromResourceAssembler
{
    public static InviteUserCommand ToCommandFromResource(InviteUserResource resource, int businessId)
    {
        return new InviteUserCommand(resource.Email, resource.Password, resource.Name, resource.LastName,
            businessId, resource.RoleId, resource.Phone);
    }
}
