namespace Kipu.Platform.Dashboard.Domain.Model.Queries;

/// <summary>Ranks by total units sold (StockMovement rows of type SALE), all-time — replaces the old "top stock" widget per X6 #6.</summary>
public record GetTopSellingProductsQuery(int BusinessId, int Count = 5);

public record TopSellingProductResult(int ProductId, string ProductName, decimal TotalSold);
