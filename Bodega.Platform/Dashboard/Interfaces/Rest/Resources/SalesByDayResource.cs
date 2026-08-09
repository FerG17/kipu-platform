namespace Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

public record SalesByDayResource(DateOnly Date, decimal Total);
