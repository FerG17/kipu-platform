namespace Kipu.Platform.Suppliers.Domain.Model.Entities;

/// <summary>
///     A line of a purchase order. Lives inside the PurchaseOrder aggregate
///     boundary — created together with its parent, same pattern as Sales'
///     SaleDetail.
///
///     DeliveryStatus/DeliveryTrackingNum are desnormalized strings kept only
///     for compatibility/history — the real, live delivery state is resolved
///     via Delivery Tracking (a future phase), not written here by hand
///     going forward (see architecture doc §6.6).
/// </summary>
public class PurchaseOrderDetail(
    int purchaseId,
    int productId,
    int quantity,
    decimal unitPrice,
    decimal discount,
    string deliveryStatus,
    string deliveryTrackingNum,
    string? batchLabel = null)
{
    public PurchaseOrderDetail() : this(0, 0, 0, 0, 0, string.Empty, string.Empty)
    {
    }

    public int Id { get; }
    public int PurchaseId { get; private set; } = purchaseId;
    public int ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;
    public decimal UnitPrice { get; private set; } = unitPrice;
    public decimal Discount { get; private set; } = discount;
    public string DeliveryStatus { get; private set; } = deliveryStatus;
    public string DeliveryTrackingNum { get; private set; } = deliveryTrackingNum;

    /// <summary>Lot name the owner wants the batch opened on RECEIVED to carry (X6 #3+#10+#11) — carried as plain text on the line since a PO's batch doesn't exist yet when the line itself is created.</summary>
    public string? BatchLabel { get; private set; } = batchLabel;

    public decimal Subtotal => Quantity * UnitPrice * (1 - Discount);
}
