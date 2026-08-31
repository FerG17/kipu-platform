namespace Kipu.Platform.Suppliers.Domain.Model.Entities;

/// <summary>
///     One scheduled cuota of a SupplierPaymentPlan — mirrors Sales'
///     PaymentInstallment (X6 #7), for a credit purchase order instead of a
///     credit sale (X6 #12). Dates/amounts here are suggested by the system
///     (proportional split + suggested cadence, decision 12) but fully
///     editable before and after the plan is created.
/// </summary>
public class SupplierPaymentInstallment(int supplierPaymentPlanId, int number, DateOnly dueDate, decimal amount)
{
    public SupplierPaymentInstallment() : this(0, 1, default, 0m)
    {
    }

    public int Id { get; }
    public int SupplierPaymentPlanId { get; private set; } = supplierPaymentPlanId;

    /// <summary>1-based order within the plan — DueDate is the real payment order, Number just labels the row.</summary>
    public int Number { get; private set; } = number;

    public DateOnly DueDate { get; private set; } = dueDate;
    public decimal Amount { get; private set; } = amount;
    public bool IsPaid { get; private set; }

    /// <summary>Caller (SupplierPaymentPlan.RegisterPayment) is responsible for only marking the earliest unpaid installment.</summary>
    public SupplierPaymentInstallment MarkPaid()
    {
        IsPaid = true;
        return this;
    }

    /// <summary>Undoes MarkPaid — used when the SupplierInstallmentPayment that paid this cuota gets reversed.</summary>
    public SupplierPaymentInstallment MarkUnpaid()
    {
        IsPaid = false;
        return this;
    }

    /// <summary>
    ///     Edits date/amount of a cuota that hasn't been paid yet — allowed
    ///     even when other cuotas in the same plan already have been (X6
    ///     #12, mirrors decision 5 from #7). Caller (SupplierPaymentPlanCommandService)
    ///     is responsible for rejecting this when IsPaid and for re-validating
    ///     the plan's total.
    /// </summary>
    public SupplierPaymentInstallment UpdateSchedule(DateOnly dueDate, decimal amount)
    {
        DueDate = dueDate;
        Amount = amount;
        return this;
    }
}
