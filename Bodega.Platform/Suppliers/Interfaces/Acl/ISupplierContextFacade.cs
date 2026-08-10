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

    /// <summary>
    ///     "Name LastName", same composition as GetPurchaseOrderDetailInfo —
    ///     used by Dashboard's "STOCK_MOVEMENTS" report to filter by
    ///     supplier: StockMovement only stores a free-text Supplier string
    ///     (set from this same name when Suppliers marks a purchase order
    ///     RECEIVED), so filtering by SupplierId means resolving the name
    ///     here first, then matching it against that text field.
    /// </summary>
    Task<string?> GetSupplierName(int supplierId, CancellationToken cancellationToken);
}
