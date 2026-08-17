using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class ResetPasswordCommandFromResourceAssembler
{
    public static ResetPasswordCommand ToCommandFromResource(ResetPasswordResource resource)
    {
        return new ResetPasswordCommand(resource.Email, resource.Code, resource.NewPassword);
    }
}
