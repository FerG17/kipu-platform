namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record UpdateProductResource(string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null);
