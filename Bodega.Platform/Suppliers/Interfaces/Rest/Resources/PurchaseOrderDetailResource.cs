namespace Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

public record PurchaseOrderDetailResource(
    int Id,
    int PurchaseId,
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal,
    string DeliveryStatus,
    string DeliveryTrackingNum);
