namespace Bodega.Platform.Suppliers.Domain.Model.Errors;

public enum SuppliersError
{
    SupplierNotFound,
    PurchaseOrderNotFound,
    InvalidStatusTransition,
    EmptyPurchaseOrderLines,
    DatabaseError
}
