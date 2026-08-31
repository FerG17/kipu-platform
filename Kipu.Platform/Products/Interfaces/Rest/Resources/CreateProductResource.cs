using Kipu.Platform.Products.Domain.Model.Aggregates;

namespace Kipu.Platform.Products.Interfaces.Rest.Resources;

public record CreateProductResource(string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null, string UnitOfSale = ProductUnitOfSale.Unit,
    string UnidadDeMedida = ProductUnidadDeMedida.Unidad, string Presentacion = ProductPresentacion.Unidad);
