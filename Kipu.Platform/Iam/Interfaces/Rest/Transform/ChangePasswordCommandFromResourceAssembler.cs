using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommandFromResource(ChangePasswordResource resource, int userId)
    {
        return new ChangePasswordCommand(userId, resource.CurrentPassword, resource.NewPassword);
    }
}
