namespace Bodega.Platform.Products.Interfaces.Rest.Resources;

public record UpdateWarehouseResource(string Name, string Code, string Address, string Capacity, bool Active);
