using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class UpdateBusinessCommandFromResourceAssembler
{
    public static UpdateBusinessCommand ToCommandFromResource(UpdateBusinessResource resource, int businessId)
    {
        return new UpdateBusinessCommand(businessId, resource.Name, resource.Type, resource.Address, resource.Ruc);
    }
}
