using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class InventoryItemResourceFromEntityAssembler
{
    public static InventoryItemResource ToResourceFromEntity(InventoryItem item)
    {
        return new InventoryItemResource(item.Id, item.ProductId, item.WarehouseId, item.BusinessId, item.StockUnit,
            item.MinimumStock, item.UpdatedAt);
    }
}
