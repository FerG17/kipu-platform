namespace Kipu.Platform.Suppliers.Domain.Model.Errors;

public enum SuppliersError
{
    SupplierNotFound,
    PurchaseOrderNotFound,
    ProductNotFound,
    InvalidStatusTransition,
    EmptyPurchaseOrderLines,
    InvalidPurchaseOrderLine,

    /// <summary>Date/ExpectedDate outside a plausible calendar range, or ExpectedDate earlier than Date.</summary>
    InvalidPurchaseOrderDate,

    /// <summary>Currency outside its allowed values, or Description too long.</summary>
    InvalidPurchaseOrderData,

    /// <summary>Supplier name/ruc/email/phone outside what its columns accept.</summary>
    InvalidSupplierData,

    /// <summary>Deactivating a supplier with a PENDING or DELAYED purchase order — those still need this supplier to be reachable.</summary>
    SupplierHasPendingOrders,

    /// <summary>Another request changed the same row first — a conflict the caller can retry (409), not a server fault (500).</summary>
    ConcurrentModification,
    DatabaseError,

    // ─── Credit purchase orders (X6 #12) ────────────────────────────────────
    SupplierPaymentPlanNotFound,
    SupplierPaymentPlanAlreadyExists,

    /// <summary>A payment plan can't be attached to (or paid against) a cancelled purchase order.</summary>
    PurchaseOrderCancelled,
    SupplierInstallmentsFullyPaid,
    SupplierPaymentPlanCancelled,

    /// <summary>The schedule's cuota amounts don't add up exactly to the purchase order's total (X6 #12, no margin allowed).</summary>
    SupplierInstallmentAmountMismatch,

    /// <summary>The installment schedule is structurally invalid (empty, non-positive amount, or missing date).</summary>
    InvalidSupplierInstallmentSchedule,

    /// <summary>UpdateSupplierPaymentInstallmentCommand against a cuota id that doesn't belong to the plan.</summary>
    SupplierInstallmentNotFound,

    /// <summary>UpdateSupplierPaymentInstallmentCommand against a cuota that's already been paid.</summary>
    SupplierInstallmentAlreadyPaid,

    /// <summary>RevertSupplierInstallmentPaymentCommand against a plan with no unreversed payment to undo.</summary>
    NoSupplierPaymentToRevert
}
