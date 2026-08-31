namespace Kipu.Platform.Suppliers.Domain.Model.Entities;

/// <summary>
///     One installment actually paid against a SupplierPaymentPlan — mirrors
///     Sales' InstallmentPayment (X6 #7), for a credit purchase order (X6
///     #12). Child entity of that aggregate — never queried independently,
///     always reached through its plan.
/// </summary>
public class SupplierInstallmentPayment(int supplierPaymentPlanId, int? supplierPaymentInstallmentId, decimal amount, int paidByUserId)
{
    public SupplierInstallmentPayment() : this(0, null, 0m, 0)
    {
    }

    public int Id { get; }
    public int SupplierPaymentPlanId { get; private set; } = supplierPaymentPlanId;

    /// <summary>Which scheduled cuota this payment fulfilled — used by RevertLastPayment to know which one to mark unpaid again.</summary>
    public int? SupplierPaymentInstallmentId { get; private set; } = supplierPaymentInstallmentId;

    /// <summary>
    ///     Taken from the matching SupplierPaymentInstallment.Amount at the
    ///     moment the payment is registered — never a value the caller types
    ///     in, so there is nothing here to trust from the client.
    /// </summary>
    public decimal Amount { get; private set; } = amount;

    public DateTimeOffset PaidAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Who registered this payment.</summary>
    public int PaidByUserId { get; private set; } = paidByUserId;

    /// <summary>
    ///     Set when this specific payment is undone — the row is kept, not
    ///     deleted, so the mistake and its correction both stay in the
    ///     record. A reversed payment no longer counts toward
    ///     SupplierPaymentPlan.PaidInstallments.
    /// </summary>
    public bool IsReversed { get; private set; }

    public DateTimeOffset? ReversedAt { get; private set; }
    public int? ReversedByUserId { get; private set; }

    /// <summary>Caller (SupplierPaymentPlan.RevertLastPayment) is responsible for only ever reversing the most recent unreversed payment.</summary>
    public SupplierInstallmentPayment Reverse(int reversedByUserId)
    {
        IsReversed = true;
        ReversedAt = DateTimeOffset.UtcNow;
        ReversedByUserId = reversedByUserId;
        return this;
    }
}
