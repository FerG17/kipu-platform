namespace Bodega.Platform.Sales.Domain.Model.Errors;

public enum SalesError
{
    SaleNotFound,
    CustomerNotFound,
    ProductNotFound,
    InsufficientStock,
    SaleAlreadyCancelled,
    InvalidStatusTransition,
    EmptySaleLines,
    InvalidSaleLine,
    PaymentPlanNotFound,
    PaymentPlanAlreadyExists,
    InstallmentsFullyPaid,
    InvalidInstallmentCount,
    DatabaseError
}
