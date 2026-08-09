using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Bodega.Platform.Suppliers.Interfaces.Rest.Transform;

public static class CreatePurchaseOrderCommandFromResourceAssembler
{
    public static CreatePurchaseOrderCommand ToCommandFromResource(CreatePurchaseOrderResource resource, int businessId)
    {
        var lines = resource.Lines
            .Select(line => new PurchaseOrderLineCommand(line.ProductId, line.Quantity, line.UnitPrice, line.Discount))
            .ToList();
        return new CreatePurchaseOrderCommand(businessId, resource.SupplierId, resource.Date, resource.ExpectedDate,
            resource.Currency, resource.Description, lines);
    }
}
