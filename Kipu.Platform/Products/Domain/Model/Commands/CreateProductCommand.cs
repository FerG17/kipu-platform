using Kipu.Platform.Products.Domain.Model.Aggregates;

namespace Kipu.Platform.Products.Domain.Model.Commands;

/// <summary>SupplierIds: the suppliers this product can be sourced from — zero or more, defaults to none.</summary>
public record CreateProductCommand(int BusinessId, string Name, string Description, string Category, decimal BasePrice,
    string? Barcode = null, IReadOnlyCollection<int>? SupplierIds = null, string UnitOfSale = ProductUnitOfSale.Unit,
    string UnidadDeMedida = ProductUnidadDeMedida.Unidad, string Presentacion = ProductPresentacion.Unidad);
