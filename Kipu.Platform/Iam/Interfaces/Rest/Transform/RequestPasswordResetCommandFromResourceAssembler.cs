using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class RequestPasswordResetCommandFromResourceAssembler
{
    public static RequestPasswordResetCommand ToCommandFromResource(ForgotPasswordResource resource)
    {
        return new RequestPasswordResetCommand(resource.Email);
    }
}
