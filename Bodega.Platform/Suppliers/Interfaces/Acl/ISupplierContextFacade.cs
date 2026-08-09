namespace Bodega.Platform.Suppliers.Interfaces.Acl;

/// <summary>
///     The only way another bounded context may reach into Supplier &amp;
///     Replenishment Management — never direct repository/DbContext access.
/// </summary>
public interface ISupplierContextFacade
{
    /// <summary>
    ///     Resolves the supplier name and product for a purchase order line —
    ///     used by Delivery Tracking to autofill CreateDeliveryCommand when a
    ///     PurchaseDetailId is provided.
    /// </summary>
    Task<(string SupplierName, int ProductId)?> GetPurchaseOrderDetailInfo(int purchaseDetailId, CancellationToken cancellationToken);
}
