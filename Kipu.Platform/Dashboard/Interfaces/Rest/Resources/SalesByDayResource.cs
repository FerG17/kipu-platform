namespace Kipu.Platform.Dashboard.Interfaces.Rest.Resources;

public record SalesByDayResource(DateOnly Date, decimal Total);
