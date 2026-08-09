using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class WarehouseResourceFromEntityAssembler
{
    public static WarehouseResource ToResourceFromEntity(Warehouse warehouse)
    {
        return new WarehouseResource(warehouse.Id, warehouse.BusinessId, warehouse.Name, warehouse.Code,
            warehouse.Address, warehouse.Status, warehouse.Capacity);
    }
}
