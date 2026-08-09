using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class CreateSaleCommandFromResourceAssembler
{
    public static CreateSaleCommand ToCommandFromResource(CreateSaleResource resource, int businessId)
    {
        var lines = resource.Lines
            .Select(line => new SaleLineCommand(line.ProductId, line.Quantity, line.UnitPrice, line.Discount))
            .ToList();
        return new CreateSaleCommand(businessId, resource.CustomerId, resource.PaymentMethod, resource.Currency,
            resource.Description, lines);
    }
}
