namespace Kipu.Platform.Dashboard.Domain.Model.Queries;

/// <summary>Ranks by real InventoryItem.currentStock — never by quantity sold (bug fixed per the handoff, architecture doc §6.8).</summary>
public record GetTopStockProductsQuery(int BusinessId, int Count = 5);

public record TopStockProductResult(int ProductId, string ProductName, int TotalStock);
