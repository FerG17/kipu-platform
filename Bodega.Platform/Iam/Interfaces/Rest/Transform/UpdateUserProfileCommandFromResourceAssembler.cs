using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class UpdateUserProfileCommandFromResourceAssembler
{
    public static UpdateUserProfileCommand ToCommandFromResource(UpdateUserProfileResource resource, int userId)
    {
        return new UpdateUserProfileCommand(userId, resource.Name, resource.LastName, resource.Phone);
    }
}
