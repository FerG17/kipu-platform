namespace Bodega.Platform.Sales.Domain.Model.Queries;

public record GetAllSalesByBusinessIdQuery(int BusinessId, DateOnly? DateFrom = null, DateOnly? DateTo = null);
