namespace Bodega.Platform.Sales.Domain.Model.Errors;

public enum SalesError
{
    SaleNotFound,
    CustomerNotFound,
    InsufficientStock,
    SaleAlreadyCancelled,
    EmptySaleLines,
    DatabaseError
}
