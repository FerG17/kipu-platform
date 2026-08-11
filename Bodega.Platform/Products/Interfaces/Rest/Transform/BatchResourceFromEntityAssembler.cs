using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class BatchResourceFromEntityAssembler
{
    /// <summary>
    ///     `today` is passed in rather than read from the system clock: it has
    ///     to be the bodega's local date (IBusinessClock), or the days-to-expiry
    ///     shown here drifts a day out of step with the alerts every evening.
    /// </summary>
    public static BatchResource ToResourceFromEntity(Batch batch, DateOnly today)
    {
        return new BatchResource(batch.Id, batch.ProductId, batch.BusinessId, batch.Expiration, batch.PurchasePrice,
            batch.Status, batch.InventoryId, batch.DaysToExpiry(today), batch.IsExpired(today), batch.IsExpiringSoon(today));
    }
}
