namespace Kipu.Platform.Sales.Domain.Model.Queries;

/// <summary>Bulk lookup for a page of sales — see SalesController.GetSales, avoids one query per credit sale.</summary>
public record GetPaymentPlansBySaleIdsQuery(IEnumerable<int> SaleIds);
