using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Queries;

namespace Kipu.Platform.Sales.Application.QueryServices;

public interface ISaleQueryService
{
    Task<IEnumerable<Sale>> Handle(GetAllSalesByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Sale?> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken);
    Task<decimal> Handle(GetTotalRevenueByBusinessIdQuery query, CancellationToken cancellationToken);
}
