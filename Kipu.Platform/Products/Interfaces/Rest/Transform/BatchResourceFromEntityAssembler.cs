using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class BatchResourceFromEntityAssembler
{
    /// <summary>
    ///     Both `today` and `thresholdDays` are passed in rather than derived
    ///     here: today has to be the bodega's local date (IBusinessClock) or
    ///     days-to-expiry drifts a day out of step every evening, and the
    ///     threshold has to be the business's configured AlertRule value
    ///     (IAlertsContextFacade) or this reports isExpiringSoon:false for a
    ///     batch that already has a live EXPIRATION alert.
    /// </summary>
    public static BatchResource ToResourceFromEntity(Batch batch, DateOnly today, int thresholdDays)
    {
        return new BatchResource(batch.Id, batch.ProductId, batch.BusinessId, batch.Expiration, batch.PurchasePrice,
            batch.Status, batch.InventoryId, batch.DaysToExpiry(today), batch.IsExpired(today),
            batch.IsExpiringSoon(today, thresholdDays));
    }
}
