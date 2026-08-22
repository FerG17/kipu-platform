using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Shared.Domain.Model.Entities;

namespace Kipu.Platform.Sales.Domain.Model.Aggregates;

public static class SaleStatus
{
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";

    /// <summary>
    ///     A sale sold on installments (see SalePaymentMethod.Credit). Nothing was
    ///     collected at checkout, so — unlike Paid — its TotalAmount is never
    ///     counted as revenue on its own; revenue comes from the
    ///     InstallmentPayment rows registered against its PaymentPlan as they
    ///     actually get paid (see SalesContextFacade). A Credit sale stays
    ///     Credit for its whole life, even once fully paid — there is no
    ///     transition back to Paid, so a payment plan's history stays
    ///     attached to it forever instead of needing to be looked up
    ///     differently depending on how much has been collected so far.
    /// </summary>
    public const string Credit = "CREDIT";

    // No OPEN/cart status: the current POS flow always creates a confirmed
    // sale at checkout (stock is validated and decremented atomically with
    // creation) — add it back only if a hold-cart feature is ever built.
}

/// <summary>
///     The methods a sale can be checked out with. Free text on the wire
///     (see CreateSaleCommandValidator's whitelist), not a real enum — these
///     constants exist so Sale/the validator don't each hardcode their own
///     copy of the same four (now five) literal strings.
/// </summary>
public static class SalePaymentMethod
{
    public const string Cash = "CASH";
    public const string Card = "CARD";
    public const string Yape = "YAPE";
    public const string Plin = "PLIN";

    /// <summary>Nothing is collected at checkout — see SaleStatus.Credit, which a sale created with this method is born into.</summary>
    public const string Credit = "CREDIT";
}

/// <summary>
///     The sale aggregate. TotalAmount is always the server-computed sum of
///     its lines' subtotals — never trust a total sent by the client.
/// </summary>
public class Sale : IVersionedEntity
{
    private readonly List<SaleDetail> _saleDetails = [];

    public Sale(int businessId, int? customerId, string paymentMethod, string currency, string description,
        string? idempotencyKey = null)
    {
        BusinessId = businessId;
        CustomerId = customerId;
        PaymentMethod = paymentMethod;
        Currency = currency;
        Description = description;
        IdempotencyKey = idempotencyKey;
        Status = paymentMethod == SalePaymentMethod.Credit ? SaleStatus.Credit : SaleStatus.Paid;
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

    /// <summary>
    ///     Client-generated key (one per checkout attempt, reused across
    ///     retries of that same attempt — see the POS store's startNewSale).
    ///     A repeat CreateSaleCommand carrying a key that already exists for
    ///     this business returns the existing sale instead of creating a
    ///     second one, so a request that succeeded but whose response never
    ///     reached the client (dropped connection, F5 mid-flight) can't be
    ///     double-charged by a retry.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    ///     Optimistic-concurrency token — what stops the same sale being
    ///     cancelled by several requests at once, each returning its units to
    ///     stock. See <see cref="IVersionedEntity" />.
    /// </summary>
    public int Version { get; set; }

    public IReadOnlyCollection<SaleDetail> SaleDetails => _saleDetails.AsReadOnly();

    public Sale AddLine(int productId, decimal quantity, decimal unitPrice, decimal discount)
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
