namespace Bodega.Platform.Sales.Domain.Model.Errors;

public enum SalesError
{
    SaleNotFound,
    CustomerNotFound,
    ProductNotFound,
    InsufficientStock,
    SaleAlreadyCancelled,
    EmptySaleLines,
    InvalidSaleLine,
    PaymentPlanNotFound,
    PaymentPlanAlreadyExists,
    InstallmentsFullyPaid,
    InvalidInstallmentCount,
    DatabaseError
}
