namespace Kipu.Platform.Sales.Domain.Model.Entities;

/// <summary>
///     One installment actually paid against a PaymentPlan — child entity of
///     that aggregate (no BusinessId of its own, same boundary as
///     Sale/SaleDetail: never queried independently, always reached through
///     its plan). Exists so a payment leaves a real record — who registered
///     it, for how much, when — instead of PaymentPlan.PaidInstallments
///     being a bare counter nothing could reconcile or undo.
/// </summary>
public class InstallmentPayment(int paymentPlanId, decimal amount, int paidByUserId)
{
    public InstallmentPayment() : this(0, 0m, 0)
    {
    }

    public int Id { get; }
    public int PaymentPlanId { get; private set; } = paymentPlanId;

    /// <summary>
    ///     Always Sale.TotalAmount / PaymentPlan.TotalInstallments, computed
    ///     server-side when the payment is registered (with any rounding
    ///     remainder folded into the last installment) — never a value the
    ///     cashier types in, so there is nothing here to trust from the client.
    /// </summary>
    public decimal Amount { get; private set; } = amount;

    public DateTimeOffset PaidAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Who registered this payment — the cashier at the till, not necessarily the plan's owner.</summary>
    public int PaidByUserId { get; private set; } = paidByUserId;

    /// <summary>
    ///     Set when this specific payment is undone (a double-click, a
    ///     mis-registered installment) — the row is kept, not deleted, so the
    ///     mistake and its correction both stay in the record. A reversed
    ///     payment no longer counts toward PaymentPlan.PaidInstallments or
    ///     toward revenue (see SalesContextFacade).
    /// </summary>
    public bool IsReversed { get; private set; }

    public DateTimeOffset? ReversedAt { get; private set; }
    public int? ReversedByUserId { get; private set; }

    /// <summary>Caller (PaymentPlan.RevertLastPayment) is responsible for only ever reversing the most recent unreversed payment.</summary>
    public InstallmentPayment Reverse(int reversedByUserId)
    {
        IsReversed = true;
        ReversedAt = DateTimeOffset.UtcNow;
        ReversedByUserId = reversedByUserId;
        return this;
    }
}
