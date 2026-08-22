using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Sales.Application.QueryServices;

public interface ISaleQueryService
{
    Task<IEnumerable<Sale>> Handle(GetAllSalesByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<PagedResult<Sale>> Handle(GetSalesPageByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken);
    Task<decimal> Handle(GetTotalRevenueByBusinessIdQuery query, CancellationToken cancellationToken);
}
