namespace Bodega.Platform.Products.Domain.Model.Commands;

public record UpdateWarehouseCommand(int WarehouseId, string Name, string Code, string Address, string Capacity, bool Active);
