namespace Kipu.Platform.Products.Domain.Model.Queries;

/// <summary>Backs the product list — one query for every product's supplier tags, grouped by ProductId, instead of one per product.</summary>
public record GetProductSupplierIdsByBusinessIdQuery(int BusinessId);
