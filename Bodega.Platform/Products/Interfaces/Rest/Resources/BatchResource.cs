namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

/// <summary>
///     DaysToExpiry/IsExpired/IsExpiringSoon are computed server-side (see
///     BatchResourceFromEntityAssembler, backed by Batch/ExpirationRules —
///     the same rules Alerts uses) so every client renders the exact same
///     "vence en N días" instead of each doing its own date math and
///     risking timezone/off-by-one drift from what actually triggers alerts.
/// </summary>
public record BatchResource(int Id, int ProductId, int BusinessId, DateOnly? Expiration, decimal PurchasePrice,
    string Status, int? InventoryId, int? DaysToExpiry, bool IsExpired, bool IsExpiringSoon);
