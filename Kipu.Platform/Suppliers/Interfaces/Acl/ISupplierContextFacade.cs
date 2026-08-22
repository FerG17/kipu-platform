namespace Kipu.Platform.Suppliers.Interfaces.Acl;

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

    /// <summary>
    ///     Filters supplierIds down to the ones that are real and belong to
    ///     this business — used by Products to validate a product's supplier
    ///     tags before linking them (ProductSupplier.SupplierId has no
    ///     database FK to Supplier, since Suppliers is a separate bounded
    ///     context, so this existence/ownership check is the only thing
    ///     standing between "tagged" and "tagged with someone else's id").
    /// </summary>
    Task<IReadOnlyCollection<int>> FilterExistingSupplierIds(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken);
}
