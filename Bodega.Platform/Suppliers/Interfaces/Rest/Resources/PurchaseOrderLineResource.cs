namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record PurchaseOrderLineResource(int ProductId, int Quantity, decimal UnitPrice, decimal Discount);
