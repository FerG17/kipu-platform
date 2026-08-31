namespace Kipu.Platform.Sales.Domain.Model.Errors;

public enum SalesError
{
    SaleNotFound,
    CustomerNotFound,
    ProductNotFound,

    /// <summary>The product exists but is deactivated — it can no longer be sold.</summary>
    ProductInactive,
    InsufficientStock,
    SaleAlreadyCancelled,
    InvalidStatusTransition,
    EmptySaleLines,
    InvalidSaleLine,

    /// <summary>A sale line asked for a fractional quantity against a product not marked "se vende por peso" (X5 Bloque D).</summary>
    FractionalQuantityNotAllowed,

    /// <summary>PaymentMethod/Currency outside their allowed values, or Description too long.</summary>
    InvalidSaleData,
    PaymentPlanNotFound,
    PaymentPlanAlreadyExists,
    InstallmentsFullyPaid,
    InvalidInstallmentCount,
    PaymentPlanCancelled,

    /// <summary>A payment plan can only be attached to a sale checked out with the CREDIT payment method (Sale.Status == Credit) — never a sale already paid in full.</summary>
    SaleIsNotACreditSale,

    /// <summary>RevertInstallmentPaymentCommand against a plan with no unreversed payment to undo.</summary>
    NoPaymentToRevert,

    /// <summary>The schedule's cuota amounts don't add up exactly to Sale.TotalAmount (X6 #7, decision 1 — no margin allowed).</summary>
    InstallmentAmountMismatch,

    /// <summary>UpdatePaymentInstallmentCommand against a cuota id that doesn't belong to the plan.</summary>
    InstallmentNotFound,

    /// <summary>UpdatePaymentInstallmentCommand against a cuota that's already been paid.</summary>
    InstallmentAlreadyPaid,

    /// <summary>Customer name/document/phone/email outside what its columns accept.</summary>
    InvalidCustomerData,

    /// <summary>Another customer in the same business already has this document number.</summary>
    DuplicateCustomerDocument,

    /// <summary>Another request changed the same row first — a conflict the caller can retry (409), not a server fault (500).</summary>
    ConcurrentModification,
    DatabaseError
}
