namespace Bodega.Platform.Sales.Domain.Model.Entities;

/// <summary>
///     A single line of a sale. Lives inside the Sale aggregate boundary —
///     always created/updated together with its parent Sale, never
///     independently, so the sale's total stays consistent with its lines.
/// </summary>
public class SaleDetail(int saleId, int productId, int quantity, decimal unitPrice, decimal discount)
{
    public SaleDetail() : this(0, 0, 0, 0, 0)
    {
    }

    public int Id { get; }
    public int SaleId { get; private set; } = saleId;
    public int ProductId { get; private set; } = productId;
    public int Quantity { get; private set; } = quantity;
    public decimal UnitPrice { get; private set; } = unitPrice;
    public decimal Discount { get; private set; } = discount;

    /// <summary>quantity × unitPrice × (1 - discount) — server-side, never trusted from the client.</summary>
    public decimal Subtotal => Quantity * UnitPrice * (1 - Discount);
}
