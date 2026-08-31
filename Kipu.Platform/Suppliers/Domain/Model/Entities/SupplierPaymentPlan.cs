using Kipu.Platform.Shared.Domain.Model.Entities;

namespace Kipu.Platform.Suppliers.Domain.Model.Entities;

/// <summary>
///     Credit purchase tracking (X6 #12) — mirrors Sales' PaymentPlan (X6
///     #7) exactly, backed by a real calendar of cuotas (see
///     SupplierPaymentInstallment). Attached to an existing PurchaseOrder
///     after the fact via a separate command, at most one plan per order.
///     The plan's own existence IS the "a crédito" signal (decision 3) — no
///     separate flag on PurchaseOrder.
/// </summary>
public class SupplierPaymentPlan : IVersionedEntity
{
    private readonly List<SupplierInstallmentPayment> _payments = [];
    private readonly List<SupplierPaymentInstallment> _installments = [];

    /// <summary>
    ///     EF's own materialization constructor — deliberately empty. See
    ///     Sales.PaymentPlan's parameterless constructor for why: chaining
    ///     this to the schedule-building constructor below would insert a
    ///     phantom installment into every plan EF loads from the database.
    /// </summary>
    public SupplierPaymentPlan()
    {
    }

    /// <summary>
    ///     Schedule is the cuota-by-cuota calendar the frontend built
    ///     (proportional split + suggested dates, both edited by the
    ///     purchaser) — the sum of its amounts is validated against the
    ///     purchase order's total by the caller (SupplierPaymentPlanCommandService),
    ///     not here.
    /// </summary>
    public SupplierPaymentPlan(int purchaseOrderId, int businessId, IReadOnlyList<(DateOnly DueDate, decimal Amount)> schedule)
    {
        PurchaseOrderId = purchaseOrderId;
        BusinessId = businessId;
        TotalInstallments = schedule.Count;

        for (var index = 0; index < schedule.Count; index++)
            _installments.Add(new SupplierPaymentInstallment(Id, index + 1, schedule[index].DueDate, schedule[index].Amount));
    }

    public int Id { get; }
    public int PurchaseOrderId { get; private set; }
    public int BusinessId { get; private set; }
    public int TotalInstallments { get; private set; }
    public int PaidInstallments { get; private set; }

    /// <summary>The audit trail behind PaidInstallments — every payment ever registered against this plan, reversed ones included.</summary>
    public IReadOnlyCollection<SupplierInstallmentPayment> Payments => _payments.AsReadOnly();

    /// <summary>The cuota-by-cuota calendar this plan was created with — see SupplierPaymentInstallment.</summary>
    public IReadOnlyCollection<SupplierPaymentInstallment> Installments => _installments.AsReadOnly();

    /// <summary>Set when the purchase order this plan belongs to gets cancelled — the plan itself is never deleted, it just stops accepting payments.</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>Optimistic-concurrency token — see <see cref="IVersionedEntity" />.</summary>
    public int Version { get; set; }

    public bool IsFullyPaid => PaidInstallments >= TotalInstallments;

    /// <summary>
    ///     Pays the earliest unpaid cuota by DueDate (never an arbitrary one
    ///     — the frontend shows a "pay ahead of schedule" confirmation
    ///     before calling this). Caller is responsible for rejecting this
    ///     when already fully paid or cancelled.
    /// </summary>
    public SupplierPaymentPlan RegisterPayment(int paidByUserId)
    {
        var nextInstallment = _installments.Where(installment => !installment.IsPaid)
            .OrderBy(installment => installment.DueDate)
            .ThenBy(installment => installment.Number)
            .First();

        nextInstallment.MarkPaid();
        PaidInstallments++;
        _payments.Add(new SupplierInstallmentPayment(Id, nextInstallment.Id, nextInstallment.Amount, paidByUserId));
        return this;
    }

    /// <summary>Undoes the most recent unreversed payment. Caller (SupplierPaymentPlanCommandService) is responsible for rejecting this when there is nothing left to revert.</summary>
    public SupplierPaymentPlan RevertLastPayment(int reversedByUserId)
    {
        var lastPayment = _payments.Where(payment => !payment.IsReversed)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .First();
        lastPayment.Reverse(reversedByUserId);

        var paidInstallment = _installments.FirstOrDefault(installment => installment.Id == lastPayment.SupplierPaymentInstallmentId);
        paidInstallment?.MarkUnpaid();

        PaidInstallments--;
        return this;
    }

    /// <summary>Whether RevertLastPayment has anything to act on.</summary>
    public bool HasReversiblePayment => _payments.Any(payment => !payment.IsReversed);

    /// <summary>The cuota RegisterPayment would pay next, if any — used by the frontend to warn before paying ahead of schedule. Null once fully paid.</summary>
    public SupplierPaymentInstallment? NextUnpaidInstallment => _installments.Where(installment => !installment.IsPaid)
        .OrderBy(installment => installment.DueDate)
        .ThenBy(installment => installment.Number)
        .FirstOrDefault();

    /// <summary>
    ///     Edits an unpaid cuota's date/amount — allowed even when other
    ///     cuotas in this plan are already paid. Caller (SupplierPaymentPlanCommandService)
    ///     is responsible for rejecting an unknown/already-paid installment id
    ///     and for re-validating the plan's total.
    /// </summary>
    public SupplierPaymentInstallment? FindInstallment(int installmentId)
    {
        return _installments.FirstOrDefault(installment => installment.Id == installmentId);
    }

    /// <summary>Caller (PurchaseOrderCommandService, on purchase order cancellation) is responsible for not calling this twice.</summary>
    public SupplierPaymentPlan Cancel()
    {
        IsCancelled = true;
        return this;
    }
}
