using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Dashboard.Interfaces.Rest.Resources;

namespace Bodega.Platform.Dashboard.Interfaces.Rest.Transform;

public static class BusinessKpisResourceFromResultAssembler
{
    public static BusinessKpisResource ToResourceFromResult(BusinessKpisResult result)
    {
        return new BusinessKpisResource(result.TotalProducts, result.LowStockCount, result.ExpiringSoonCount,
            result.InventoryValue, result.TotalSales, result.StockHealthPercentage);
    }
}
