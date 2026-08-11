namespace Bodega.Platform.Products.Domain.Model.Errors;

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
    DatabaseError
}
