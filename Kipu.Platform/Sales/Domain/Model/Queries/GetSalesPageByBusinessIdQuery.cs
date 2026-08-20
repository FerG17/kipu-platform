using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Sales.Domain.Model.Queries;

/// <summary>
///     Backs the GetSales collection endpoint only (X4 S3). Distinct from
///     GetAllSalesByBusinessIdQuery, which stays unpaged for internal callers
///     (SalesContextFacade: revenue calc, Excel export) that genuinely need
///     the whole set.
/// </summary>
public record GetSalesPageByBusinessIdQuery(int BusinessId, DateOnly? DateFrom, DateOnly? DateTo, PageRequest Page);
