namespace Bodega.Platform.Products.Domain.Model.Queries;

public record GetAllProductsByBusinessIdQuery(int BusinessId, string? Category = null);
