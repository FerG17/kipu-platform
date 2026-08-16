using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource)
    {
        return new SignUpCommand(resource.Email, resource.Password, resource.Name, resource.LastName, resource.Phone,
            resource.BusinessName, resource.BusinessType, resource.Ruc, resource.Address);
    }
}
