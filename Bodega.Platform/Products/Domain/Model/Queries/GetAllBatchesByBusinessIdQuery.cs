namespace Bodega.Platform.Products.Domain.Model.Queries;

/// <summary>Used for the global "which products are expiring soon" calculation across the whole business.</summary>
public record GetAllBatchesByBusinessIdQuery(int BusinessId);
