using Bodega.Platform.Suppliers.Domain.Repositories;
using Bodega.Platform.Suppliers.Interfaces.Acl;

namespace Bodega.Platform.Suppliers.Application.Acl;

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
}
