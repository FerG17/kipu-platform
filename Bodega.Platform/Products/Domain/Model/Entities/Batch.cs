using Bodega.Platform.Shared.Domain.Model.Services;

namespace Bodega.Platform.Products.Domain.Model.Entities;

public static class BatchStatus
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}

/// <summary>
///     Tracks a product's expiration date and purchase price. The frontend's
///     product form only captures one expiration date per product (no batch
///     selector UI), so there's at most one ACTIVE batch per product at a
///     time — re-editing updates it in place instead of piling up batches.
///
///     BusinessId is a deliberate addition over the original mock schema
///     (which had none, forcing the frontend to scope batches by joining
///     against the already-loaded, business-scoped products list) — a real
///     backend can and should scope directly.
/// </summary>
public class Batch(int productId, int businessId, DateOnly? expiration, decimal purchasePrice, int? inventoryId)
{
    public Batch() : this(0, 0, null, 0, null)
    {
    }

    public int Id { get; }
    public int ProductId { get; private set; } = productId;
    public int BusinessId { get; private set; } = businessId;
    public DateOnly? Expiration { get; private set; } = expiration;
    public decimal PurchasePrice { get; private set; } = purchasePrice;
    public string Status { get; private set; } = BatchStatus.Active;
    public int? InventoryId { get; private set; } = inventoryId;

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

    public Batch UpdateDetails(DateOnly? expiration, decimal purchasePrice, int? inventoryId)
    {
        Expiration = expiration;
        PurchasePrice = purchasePrice;
        InventoryId = inventoryId ?? InventoryId;
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
