using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommandFromResource(ChangePasswordResource resource, int userId)
    {
        return new ChangePasswordCommand(userId, resource.CurrentPassword, resource.NewPassword);
    }
}
