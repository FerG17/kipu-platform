namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>SupplierIds: the full desired set of suppliers for this product — replaces whatever was linked before, not a delta.</summary>
public record UpdateProductCommand(int ProductId, string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null);
