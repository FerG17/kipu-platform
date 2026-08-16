using Kipu.Platform.Dashboard.Domain.Model.Queries;
using Kipu.Platform.Dashboard.Interfaces.Rest.Resources;

namespace Kipu.Platform.Dashboard.Interfaces.Rest.Transform;

public static class BusinessKpisResourceFromResultAssembler
{
    public static BusinessKpisResource ToResourceFromResult(BusinessKpisResult result)
    {
        return new BusinessKpisResource(result.TotalProducts, result.LowStockCount, result.ExpiringSoonCount,
            result.InventoryValue, result.TotalSales, result.StockHealthPercentage);
    }
}
