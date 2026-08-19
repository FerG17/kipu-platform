namespace Kipu.Platform.Suppliers.Domain.Model.Errors;

public enum SuppliersError
{
    SupplierNotFound,
    PurchaseOrderNotFound,
    ProductNotFound,
    InvalidStatusTransition,
    EmptyPurchaseOrderLines,
    InvalidPurchaseOrderLine,

    /// <summary>Supplier name/ruc/email/phone outside what its columns accept.</summary>
    InvalidSupplierData,

    /// <summary>Deactivating a supplier with a PENDING or DELAYED purchase order — those still need this supplier to be reachable.</summary>
    SupplierHasPendingOrders,

    /// <summary>Another request changed the same row first — a conflict the caller can retry (409), not a server fault (500).</summary>
    ConcurrentModification,
    DatabaseError
}
