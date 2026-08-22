using Kipu.Platform.Dashboard.Domain.Model.Queries;

namespace Kipu.Platform.Dashboard.Application.QueryServices;

public interface IDashboardQueryService
{
    Task<BusinessKpisResult> Handle(GetBusinessKpisQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SalesByDayResult>> Handle(GetSalesByDayQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TopStockProductResult>> Handle(GetTopStockProductsQuery query, CancellationToken cancellationToken);
}
