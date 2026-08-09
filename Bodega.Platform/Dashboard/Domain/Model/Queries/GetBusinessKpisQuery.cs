namespace Bodega.Platform.Dashboard.Domain.Model.Queries;

public record GetBusinessKpisQuery(int BusinessId);

/// <summary>
///     The 6 KPIs, always computed live from Product/Inventory/Sales — never
///     from a stored `metrics`/snapshot table (architecture doc §6.8 — the
///     `/metrics` mock endpoint is dead and must not be replicated).
/// </summary>
public record BusinessKpisResult(
    int TotalProducts,
    int LowStockCount,
    int ExpiringSoonCount,
    decimal InventoryValue,
    decimal TotalSales,
    double StockHealthPercentage);
