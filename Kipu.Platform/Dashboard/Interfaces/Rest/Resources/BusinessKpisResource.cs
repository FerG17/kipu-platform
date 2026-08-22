namespace Kipu.Platform.Dashboard.Interfaces.Rest.Resources;

public record BusinessKpisResource(
    int TotalProducts,
    int LowStockCount,
    int ExpiringSoonCount,
    decimal InventoryValue,
    decimal TotalSales,
    double StockHealthPercentage);
