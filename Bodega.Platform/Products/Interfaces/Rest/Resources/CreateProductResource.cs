namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record CreateProductResource(string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null);
