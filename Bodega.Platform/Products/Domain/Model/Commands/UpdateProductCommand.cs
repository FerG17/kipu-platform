namespace Bodega.Platform.Products.Domain.Model.Commands;

public record UpdateProductCommand(int ProductId, string Name, string Description, string Category, decimal BasePrice, string? Barcode = null);
