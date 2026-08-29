using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class StockMovementResourceFromEntityAssembler
{
    public static StockMovementResource ToResourceFromEntity(StockMovement movement)
    {
        return new StockMovementResource(movement.Id, movement.ProductId, movement.BusinessId, movement.WarehouseId,
            movement.Quantity, movement.Type, movement.Supplier, movement.Note, movement.RegisteredAt,
            movement.Batch?.Id, movement.Batch?.PurchasePrice);
    }
}
