using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class InventoryItemResourceFromEntityAssembler
{
    public static InventoryItemResource ToResourceFromEntity(InventoryItem item)
    {
        return new InventoryItemResource(item.Id, item.ProductId, item.WarehouseId, item.BusinessId, item.StockUnit,
            item.MinimumStock, item.UpdatedAt);
    }
}
