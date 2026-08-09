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

    public bool IsExpiringSoon(DateOnly today)
    {
        return ExpirationRules.IsExpiringSoon(Expiration, today);
    }

    public Batch UpdateDetails(DateOnly? expiration, decimal purchasePrice, int? inventoryId)
    {
        Expiration = expiration;
        PurchasePrice = purchasePrice;
        InventoryId = inventoryId ?? InventoryId;
        return this;
    }
}
