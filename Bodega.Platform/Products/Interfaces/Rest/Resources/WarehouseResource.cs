namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record WarehouseResource(int Id, int BusinessId, string Name, string Code, string Address, string Status, string Capacity);
