using Kipu.Platform.Shared.Domain.Model.Services;

namespace Kipu.Platform.Products.Domain.Model.Entities;

public static class BatchStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

/// <summary>
///     Tracks one physical lot of a product: the quantity received, how much
///     of it is still on the shelf, its expiration date, and its purchase
///     price. A product can have several ACTIVE batches at once (X5 Bloque
///     C) — e.g. 10 units expiring in a week plus 50 units from an early
///     restock expiring in 4 months — so a batch is never rewritten in place
///     once created; each stock intake that carries an expiration/cost opens
///     a new one instead. Sales draw down RemainingQuantity across a
///     product's active batches earliest-expiration-first (FEFO).
///
///     BusinessId is a deliberate addition over the original mock schema
///     (which had none, forcing the frontend to scope batches by joining
///     against the already-loaded, business-scoped products list) — a real
///     backend can and should scope directly.
/// </summary>
public class Batch(int productId, int businessId, DateOnly? expiration, decimal purchasePrice, int quantity)
{
    public Batch() : this(0, 0, null, 0, 0)
    {
    }

    public int Id { get; }
    public int ProductId { get; private set; } = productId;
    public int BusinessId { get; private set; } = businessId;
    public DateOnly? Expiration { get; private set; } = expiration;
    public decimal PurchasePrice { get; private set; } = purchasePrice;
    public string Status { get; private set; } = BatchStatus.Active;

    /// <summary>Units originally received into this lot — fixed at creation, never changes.</summary>
    public int Quantity { get; private set; } = quantity;

    /// <summary>Units of this lot still on the shelf — decremented by FEFO sales, restored by returns into this same lot.</summary>
    public int RemainingQuantity { get; private set; } = quantity;

    /// <summary>
    ///     Real EF Core relationship (see ModelBuilderExtensions), not a
    ///     plain `int` copied by hand — a batch is always created together
    ///     with (or against an already-existing) InventoryItem in the same
    ///     SaveChanges call, so assigning through the navigation lets EF
    ///     Core's own key fixup resolve the real id even when the
    ///     InventoryItem hasn't been persisted yet. Copying `item.Id`
    ///     directly used to read 0 in that case (X5 #9's InventoryId=0 bug).
    /// </summary>
    public InventoryItem? InventoryItem { get; private set; }

    /// <summary>Read-only convenience for callers that only need the id (e.g. the API resource) — always sourced from the navigation, so it can never drift out of sync with it.</summary>
    public int? InventoryId => InventoryItem?.Id;

    public Batch LinkToInventoryItem(InventoryItem inventoryItem)
    {
        InventoryItem = inventoryItem;
        return this;
    }

    /// <summary>Days until expiration; negative when already expired. Null when no expiration date is set.</summary>
    public int? DaysToExpiry(DateOnly today)
    {
        return Expiration?.DayNumber - today.DayNumber;
    }

    public bool IsExpired(DateOnly today)
    {
        return ExpirationRules.IsExpired(Expiration, today);
    }

    /// <summary>
    ///     thresholdDays defaults to the platform-wide value, but callers
    ///     that can reach the business's AlertRule should pass its configured
    ///     one — otherwise this answers "not expiring soon" for a batch that
    ///     already has a live EXPIRATION alert against it.
    /// </summary>
    public bool IsExpiringSoon(DateOnly today, int thresholdDays = ExpirationRules.ExpiringSoonThresholdDays)
    {
        return ExpirationRules.IsExpiringSoon(Expiration, today, thresholdDays);
    }

    public bool HasStock => Status == BatchStatus.Active && RemainingQuantity > 0;

    /// <summary>FEFO sale deduction — `checked` for the same reason InventoryItem.AddStock is: a caller bug should throw loudly instead of wrapping RemainingQuantity negative and silently under-reporting this lot's stock.</summary>
    public Batch Reduce(int units)
    {
        checked
        {
            RemainingQuantity -= units;
        }

        return this;
    }

    /// <summary>Returns units to this specific lot (a cancelled sale that drew from it) — capped at Quantity, since a lot can never hold more than it originally received.</summary>
    public Batch Restore(int units)
    {
        RemainingQuantity = Math.Min(Quantity, RemainingQuantity + units);
        return this;
    }

    /// <summary>
    ///     Retires the batch — the goods were thrown out, returned to the
    ///     supplier, or otherwise taken off the shelf.
    ///
    ///     Until this existed there was no way to stop an expired batch from
    ///     alerting. It stayed ACTIVE forever, so the periodic sweep raised
    ///     the same "venció" alert again within hours of every time it was
    ///     resolved, and the shop had no way to make it stop.
    /// </summary>
    public Batch Discard()
    {
        Status = BatchStatus.Inactive;
        return this;
    }
}
