using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommandFromResource(SignUpResource resource)
    {
        return new SignUpCommand(resource.Email, resource.Password, resource.Name, resource.LastName, resource.Phone,
            resource.BusinessName, resource.BusinessType, resource.Ruc, resource.Address);
    }
}
