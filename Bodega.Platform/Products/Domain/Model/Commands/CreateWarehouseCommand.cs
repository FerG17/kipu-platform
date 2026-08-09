namespace Bodega.Platform.Products.Domain.Model.Commands;

public record CreateWarehouseCommand(int BusinessId, string Name, string Code, string Address, string Capacity);
