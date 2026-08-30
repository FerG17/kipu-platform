namespace Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

public record PurchaseOrderLineResource(int ProductId, int Quantity, decimal UnitPrice, decimal Discount, string? BatchLabel = null);
