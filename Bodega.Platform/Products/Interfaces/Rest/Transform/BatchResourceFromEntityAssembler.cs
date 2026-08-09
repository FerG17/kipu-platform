using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class BatchResourceFromEntityAssembler
{
    public static BatchResource ToResourceFromEntity(Batch batch)
    {
        return new BatchResource(batch.Id, batch.ProductId, batch.BusinessId, batch.Expiration, batch.PurchasePrice,
            batch.Status, batch.InventoryId);
    }
}
