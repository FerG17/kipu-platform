using Bodega.Platform.Sales.Domain.Model.Entities;

namespace Bodega.Platform.Sales.Domain.Model.Aggregates;

public static class SaleStatus
{
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";

    // No OPEN/cart status: the current POS flow always creates a fully paid
    // sale at checkout (stock is validated and decremented atomically with
    // creation) — add it back only if a hold-cart feature is ever built.
}

/// <summary>
///     The sale aggregate. TotalAmount is always the server-computed sum of
///     its lines' subtotals — never trust a total sent by the client.
/// </summary>
public class Sale
{
    private readonly List<SaleDetail> _saleDetails = [];

    public Sale(int businessId, int? customerId, string paymentMethod, string currency, string description)
    {
        BusinessId = businessId;
        CustomerId = customerId;
        PaymentMethod = paymentMethod;
        Currency = currency;
        Description = description;
        Status = SaleStatus.Paid;
        Date = DateTimeOffset.UtcNow;
    }

    public Sale()
    {
        PaymentMethod = string.Empty;
        Currency = string.Empty;
        Description = string.Empty;
        Status = SaleStatus.Paid;
    }

    public int Id { get; }
    public int BusinessId { get; private set; }
    public int? CustomerId { get; private set; }
    public string Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string PaymentMethod { get; private set; }
    public DateTimeOffset Date { get; private set; }
    public string Description { get; private set; }
    public string Currency { get; private set; }

    public IReadOnlyCollection<SaleDetail> SaleDetails => _saleDetails.AsReadOnly();

    public Sale AddLine(int productId, int quantity, decimal unitPrice, decimal discount)
    {
        _saleDetails.Add(new SaleDetail(Id, productId, quantity, unitPrice, discount));
        RecalculateTotal();
        return this;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _saleDetails.Sum(detail => detail.Subtotal);
    }

    public Sale Cancel()
    {
        Status = SaleStatus.Cancelled;
        return this;
    }
}
