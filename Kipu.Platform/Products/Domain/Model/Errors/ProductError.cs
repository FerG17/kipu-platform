namespace Kipu.Platform.Products.Domain.Model.Errors;

public enum ProductError
{
    ProductNotFound,
    CannotDeleteWithStock,
    WarehouseNotFound,
    WarehouseRequired,
    InventoryItemNotFound,
    InsufficientStock,
    InvalidQuantity,
    InvalidExpirationDate,
    InvalidPurchasePrice,
    BatchNotFound,
    BatchAlreadyDiscarded,

    /// <summary>The product is deactivated — it cannot receive a stock intake until it's reactivated.</summary>
    ProductInactive,

    /// <summary>A stock adjustment's delta was zero — nothing to correct.</summary>
    InvalidAdjustmentQuantity,

    /// <summary>A stock adjustment must always record why (shrinkage, breakage, theft, count fix).</summary>
    AdjustmentReasonRequired,

    /// <summary>A downward stock adjustment asked to remove more units than the warehouse actually has.</summary>
    AdjustmentExceedsAvailableStock,

    /// <summary>Name/description/category/price outside what the catalog (and its columns) accept.</summary>
    InvalidProductData,

    /// <summary>Barcode already registered on another active product of the same business.</summary>
    DuplicateBarcode,

    /// <summary>A SupplierId tag doesn't exist, or belongs to another business.</summary>
    SupplierNotFound,

    /// <summary>Name/code/address outside what the warehouse's columns accept.</summary>
    InvalidWarehouseData,

    /// <summary>Another request changed the same row first — a conflict the caller can retry (409), not a server fault (500).</summary>
    ConcurrentModification,
    DatabaseError
}
