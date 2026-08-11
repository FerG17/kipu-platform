using Bodega.Platform.Shared.Application;
using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Repositories;
using Bodega.Platform.Sales.Interfaces.Acl;

namespace Bodega.Platform.Sales.Application.Acl;

public class SalesContextFacade(ISaleRepository saleRepository, IBusinessClock businessClock) : ISalesContextFacade
{
    public async Task<decimal> GetTotalRevenue(int businessId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken)
    {
        return await saleRepository.SumPaidTotalByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken);
    }

    public async Task<IReadOnlyCollection<(DateOnly Date, decimal Total)>> GetSalesByDay(int businessId, DateOnly dateFrom,
        DateOnly dateTo, CancellationToken cancellationToken)
    {
        var sales = await saleRepository.FindAllByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken);
        return sales.Where(sale => sale.Status == SaleStatus.Paid)
            .GroupBy(sale => businessClock.ToLocalDate(sale.Date))
            .Select(group => (Date: group.Key, Total: group.Sum(sale => sale.TotalAmount)))
            .OrderBy(entry => entry.Date)
            .ToList();
    }

    public async Task<IReadOnlyCollection<SaleExportRow>> GetSalesForExport(int businessId, DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var sales = await saleRepository.FindAllByBusinessIdAsync(businessId, dateFrom, dateTo, cancellationToken);
        return sales.Where(sale => sale.Status == SaleStatus.Paid)
            .Select(sale => new SaleExportRow(sale.Id, sale.Date, sale.PaymentMethod, sale.TotalAmount, sale.Currency))
            .ToList();
    }
}
