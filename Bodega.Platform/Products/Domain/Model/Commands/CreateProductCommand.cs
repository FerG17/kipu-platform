namespace Bodega.Platform.Products.Domain.Model.Commands;

public record CreateProductCommand(int BusinessId, string Name, string Description, string Category, decimal BasePrice);
