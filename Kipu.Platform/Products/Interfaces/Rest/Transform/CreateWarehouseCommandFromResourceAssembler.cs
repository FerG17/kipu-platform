using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class CreateWarehouseCommandFromResourceAssembler
{
    public static CreateWarehouseCommand ToCommandFromResource(CreateWarehouseResource resource, int businessId)
    {
        return new CreateWarehouseCommand(businessId, resource.Name, resource.Code, resource.Address, resource.Capacity);
    }
}
