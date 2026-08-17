using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class VerifyPasswordResetCodeCommandFromResourceAssembler
{
    public static VerifyPasswordResetCodeCommand ToCommandFromResource(VerifyResetCodeResource resource)
    {
        return new VerifyPasswordResetCodeCommand(resource.Email, resource.Code);
    }
}
