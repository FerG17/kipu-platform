namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record SaleDetailResource(int Id, int SaleId, int ProductId, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Subtotal);
