using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Suppliers.Application.Acl;

public class SupplierContextFacade(
    IPurchaseOrderRepository purchaseOrderRepository,
    ISupplierRepository supplierRepository,
    ISupplierPaymentPlanRepository supplierPaymentPlanRepository)
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

    /// <summary>
    ///     Batched the same way SalesContextFacade.GetPendingInstallmentsForDueSweep
    ///     is (X6 #7): one query for plans, one for their purchase orders, one
    ///     for those orders' suppliers — instead of N+1 queries per plan.
    /// </summary>
    public async Task<IReadOnlyCollection<PendingSupplierInstallmentInfo>> GetPendingSupplierInstallmentsForDueSweep(
        CancellationToken cancellationToken)
    {
        var plansWithNextInstallment = (await supplierPaymentPlanRepository.FindAllPendingAcrossBusinessesAsync(cancellationToken))
            .Select(plan => (plan, next: plan.Installments.Where(installment => !installment.IsPaid)
                .OrderBy(installment => installment.DueDate).ThenBy(installment => installment.Number)
                .FirstOrDefault()))
            .Where(entry => entry.next != null)
            .ToList();

        var purchaseOrderIds = plansWithNextInstallment.Select(entry => entry.plan.PurchaseOrderId).Distinct().ToList();
        var ordersById = (await purchaseOrderRepository.FindAllIgnoringTenantByIdsAsync(purchaseOrderIds, cancellationToken))
            .ToDictionary(order => order.Id);

        var supplierIds = ordersById.Values.Select(order => order.SupplierId).Distinct().ToList();
        var supplierNamesById = (await supplierRepository.FindAllIgnoringTenantByIdsAsync(supplierIds, cancellationToken))
            .ToDictionary(supplier => supplier.Id, supplier => $"{supplier.Name} {supplier.LastName}".Trim());

        return plansWithNextInstallment.Select(entry =>
        {
            var order = ordersById.GetValueOrDefault(entry.plan.PurchaseOrderId);
            var supplierName = order != null ? supplierNamesById.GetValueOrDefault(order.SupplierId) : null;
            return new PendingSupplierInstallmentInfo(entry.plan.Id, entry.plan.PurchaseOrderId, entry.plan.BusinessId,
                supplierName, entry.next!.Id, entry.next.DueDate, entry.next.Amount);
        }).ToList();
    }
}
