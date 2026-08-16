using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class UpdateBusinessCommandFromResourceAssembler
{
    public static UpdateBusinessCommand ToCommandFromResource(UpdateBusinessResource resource, int businessId)
    {
        return new UpdateBusinessCommand(businessId, resource.Name, resource.Type, resource.Address, resource.Ruc);
    }
}
