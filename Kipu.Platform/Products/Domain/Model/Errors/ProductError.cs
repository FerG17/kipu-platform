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

    /// <summary>A fractional Quantity/Delta was given for a product not marked "se vende por peso" — only weight-sold products may carry a fractional stock quantity (X5 Bloque D).</summary>
    FractionalQuantityNotAllowed,
    InvalidExpirationDate,
    InvalidPurchasePrice,
    BatchNotFound,
    BatchAlreadyDiscarded,

    /// <summary>A discarded batch's expiration can no longer be edited — it's no longer on the shelf.</summary>
    BatchNotEditable,

    /// <summary>The product is deactivated — it cannot receive a stock intake until it's reactivated.</summary>
    ProductInactive,

    /// <summary>A stock adjustment's delta was zero — nothing to correct.</summary>
    InvalidAdjustmentQuantity,

    /// <summary>A stock adjustment must always record why (shrinkage, breakage, theft, count fix).</summary>
    AdjustmentReasonRequired,

    /// <summary>A downward stock adjustment asked to remove more units than the warehouse actually has.</summary>
    AdjustmentExceedsAvailableStock,

    /// <summary>A stock adjustment's reason exceeds its column length, or its delta's magnitude is implausibly large.</summary>
    InvalidAdjustmentReason,

    /// <summary>A stock intake's Supplier/Note text exceeds its column length.</summary>
    InvalidStockIntakeData,

    /// <summary>The warehouse a movement targets is deactivated — it can no longer receive or hold stock.</summary>
    WarehouseInactive,

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
