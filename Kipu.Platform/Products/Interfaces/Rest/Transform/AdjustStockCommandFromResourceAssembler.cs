using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class AdjustStockCommandFromResourceAssembler
{
    public static AdjustStockCommand ToCommandFromResource(AdjustStockResource resource, int productId, int businessId)
    {
        return new AdjustStockCommand(productId, resource.WarehouseId, businessId, resource.Delta, resource.Reason);
    }
}
