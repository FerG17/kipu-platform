using Bodega.Platform.Shared.Application;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Sales.Interfaces.Acl;

namespace Bodega.Platform.Dashboard.Application.Internal.QueryServices;

/// <summary>
///     Composes data live from Product/Inventory/Sales via their ACL facades
///     — Dashboard has no aggregates of its own (architecture doc §6.8).
///     Never reads from a stored `metrics`/snapshot table — the mock's
///     `/metrics` endpoint is dead and must not be replicated.
/// </summary>
public class DashboardQueryService(IProductContextFacade productContextFacade, ISalesContextFacade salesContextFacade,
    IBusinessClock businessClock)
    : IDashboardQueryService
{
    public async Task<BusinessKpisResult> Handle(GetBusinessKpisQuery query, CancellationToken cancellationToken)
    {
        var productKpis = await productContextFacade.GetProductKpisSnapshot(query.BusinessId, cancellationToken);
        var totalSales = await salesContextFacade.GetTotalRevenue(query.BusinessId, null, null, cancellationToken);

        // "Health" = share of the catalog that isn't currently flagged low-stock.
        var stockHealthPercentage = productKpis.TotalProducts == 0
            ? 100.0
            : (double)(productKpis.TotalProducts - productKpis.LowStockCount) / productKpis.TotalProducts * 100.0;

        return new BusinessKpisResult(
            productKpis.TotalProducts,
            productKpis.LowStockCount,
            productKpis.ExpiringSoonCount,
            productKpis.InventoryValue,
            totalSales,
            stockHealthPercentage);
    }

    public async Task<IReadOnlyCollection<SalesByDayResult>> Handle(GetSalesByDayQuery query, CancellationToken cancellationToken)
    {
        var today = businessClock.Today;
        var dateFrom = query.DateFrom ?? today.AddDays(-6);
        var dateTo = query.DateTo ?? today;

        var salesByDay = await salesContextFacade.GetSalesByDay(query.BusinessId, dateFrom, dateTo, cancellationToken);
        var totalsByDate = salesByDay.ToDictionary(entry => entry.Date, entry => entry.Total);

        var days = new List<SalesByDayResult>();
        for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
            days.Add(new SalesByDayResult(date, totalsByDate.GetValueOrDefault(date, 0m)));

        return days;
    }

    public async Task<IReadOnlyCollection<TopStockProductResult>> Handle(GetTopStockProductsQuery query,
        CancellationToken cancellationToken)
    {
        var topProducts = await productContextFacade.GetTopStockProducts(query.BusinessId, query.Count, cancellationToken);
        return topProducts.Select(product => new TopStockProductResult(product.ProductId, product.ProductName, product.TotalStock))
            .ToList();
    }
}
