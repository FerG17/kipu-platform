namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record ProductResource(int Id, int BusinessId, string Name, string Description, string Category, decimal BasePrice,
    string Status, string? Barcode, IReadOnlyCollection<int> SupplierIds);
