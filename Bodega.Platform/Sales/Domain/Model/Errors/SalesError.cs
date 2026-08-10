namespace Bodega.Platform.Sales.Domain.Model.Errors;

public enum SalesError
{
    SaleNotFound,
    CustomerNotFound,
    ProductNotFound,
    InsufficientStock,
    SaleAlreadyCancelled,
    EmptySaleLines,
    PaymentPlanNotFound,
    PaymentPlanAlreadyExists,
    InstallmentsFullyPaid,
    InvalidInstallmentCount,
    DatabaseError
}
