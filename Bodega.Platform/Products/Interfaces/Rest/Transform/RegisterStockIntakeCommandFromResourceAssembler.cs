using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class RegisterStockIntakeCommandFromResourceAssembler
{
    public static RegisterStockIntakeCommand ToCommandFromResource(RegisterStockIntakeResource resource, int productId,
        int businessId)
    {
        return new RegisterStockIntakeCommand(productId, businessId, resource.WarehouseId, resource.Quantity,
            resource.PurchasePrice, resource.Expiration, resource.Supplier, resource.Note, resource.MinimumStock);
    }
}
