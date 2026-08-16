namespace Kipu.Platform.Suppliers.Domain.Model.Commands;

public record PurchaseOrderLineCommand(int ProductId, int Quantity, decimal UnitPrice, decimal Discount);
