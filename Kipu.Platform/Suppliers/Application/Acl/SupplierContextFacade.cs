using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Suppliers.Application.Acl;

public class SupplierContextFacade(IPurchaseOrderRepository purchaseOrderRepository, ISupplierRepository supplierRepository)
    : ISupplierContextFacade
{
    public async Task<(string SupplierName, int ProductId)?> GetPurchaseOrderDetailInfo(int purchaseDetailId,
        CancellationToken cancellationToken)
    {
        var result = await purchaseOrderRepository.FindByDetailIdAsync(purchaseDetailId, cancellationToken);
        if (result == null) return null;

        var (order, detail) = result.Value;
        var supplier = await supplierRepository.FindByIdAsync(order.SupplierId, cancellationToken);
        var supplierName = supplier != null ? $"{supplier.Name} {supplier.LastName}".Trim() : string.Empty;

        return (supplierName, detail.ProductId);
    }

    public async Task<string?> GetSupplierName(int supplierId, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.FindByIdAsync(supplierId, cancellationToken);
        return supplier == null ? null : $"{supplier.Name} {supplier.LastName}".Trim();
    }

    public async Task<IReadOnlyCollection<int>> FilterExistingSupplierIds(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken)
    {
        if (supplierIds.Count == 0) return [];
        return await supplierRepository.FindExistingIdsAsync(businessId, supplierIds, cancellationToken);
    }
}
