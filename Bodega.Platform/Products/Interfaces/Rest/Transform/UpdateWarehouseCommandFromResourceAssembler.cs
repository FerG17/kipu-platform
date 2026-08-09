using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class UpdateWarehouseCommandFromResourceAssembler
{
    public static UpdateWarehouseCommand ToCommandFromResource(UpdateWarehouseResource resource, int warehouseId)
    {
        return new UpdateWarehouseCommand(warehouseId, resource.Name, resource.Code, resource.Address, resource.Capacity,
            resource.Active);
    }
}
