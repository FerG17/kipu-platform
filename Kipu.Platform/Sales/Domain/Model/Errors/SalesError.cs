namespace Kipu.Platform.Sales.Domain.Model.Errors;

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
    PaymentPlanCancelled,

    /// <summary>Customer name/document/phone/email outside what its columns accept.</summary>
    InvalidCustomerData,

    /// <summary>Another request changed the same row first — a conflict the caller can retry (409), not a server fault (500).</summary>
    ConcurrentModification,
    DatabaseError
}
