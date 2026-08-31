namespace Kipu.Platform.Sales.Domain.Model.Entities;

/// <summary>
///     One scheduled cuota of a PaymentPlan — DueDate + Amount entered by the
///     cashier when the plan is created (proportional split suggested by the
///     frontend, remainder folded into the last one), not calculated on the
///     fly at payment time. Replaces the original design's even-split
///     calculation (see PaymentPlanCommandService) with a real, editable
///     calendar (X6 #7).
/// </summary>
public class PaymentInstallment(int paymentPlanId, int number, DateOnly dueDate, decimal amount)
{
    public PaymentInstallment() : this(0, 1, default, 0m)
    {
    }

    public int Id { get; }
    public int PaymentPlanId { get; private set; } = paymentPlanId;

    /// <summary>1-based order within the plan — DueDate is the real payment order, Number just labels the row.</summary>
    public int Number { get; private set; } = number;

    public DateOnly DueDate { get; private set; } = dueDate;
    public decimal Amount { get; private set; } = amount;
    public bool IsPaid { get; private set; }

    /// <summary>Caller (PaymentPlan.RegisterPayment) is responsible for only marking the earliest unpaid installment.</summary>
    public PaymentInstallment MarkPaid()
    {
        IsPaid = true;
        return this;
    }

    /// <summary>Undoes MarkPaid — used when the InstallmentPayment that paid this cuota gets reversed.</summary>
    public PaymentInstallment MarkUnpaid()
    {
        IsPaid = false;
        return this;
    }

    /// <summary>
    ///     Edits date/amount of a cuota that hasn't been paid yet — allowed
    ///     even when other cuotas in the same plan already have been (X6 #7,
    ///     decision 5). Caller (PaymentPlanCommandService) is responsible for
    ///     rejecting this when IsPaid and for re-validating the plan's total.
    /// </summary>
    public PaymentInstallment UpdateSchedule(DateOnly dueDate, decimal amount)
    {
        DueDate = dueDate;
        Amount = amount;
        return this;
    }
}
