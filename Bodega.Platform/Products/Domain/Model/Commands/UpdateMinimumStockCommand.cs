namespace Bodega.Platform.Products.Domain.Model.Commands;

public record UpdateMinimumStockCommand(int ProductId, int MinimumStock);
