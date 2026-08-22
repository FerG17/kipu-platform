using Kipu.Platform.Products.Domain.Model.Aggregates;

namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record UpdateProductResource(string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null, string UnitOfSale = ProductUnitOfSale.Unit);
