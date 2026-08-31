using Kipu.Platform.Shared.Domain.Model.Entities;

namespace Kipu.Platform.Sales.Domain.Model.Entities;

/// <summary>
///     Credit sales tracking — how many installments a sale is split into
///     and how many have been paid, backed by a real calendar of cuotas
///     (see PaymentInstallment, X6 #7). Attached to an existing Sale after
///     the fact via a separate command, at most one plan per sale — no
///     changes to how a Sale itself is created/totaled/decremented (see
///     CreateSaleCommand, untouched).
/// </summary>
public class PaymentPlan : IVersionedEntity
{
    private readonly List<InstallmentPayment> _payments = [];
    private readonly List<PaymentInstallment> _installments = [];

    /// <summary>
    ///     EF's own materialization constructor — deliberately empty. The
    ///     other constructor builds PaymentInstallment children as a side
    ///     effect (see below); chaining this one to it used to insert a
    ///     phantom zero-amount/default-date installment into EVERY plan EF
    ///     loaded from the database, on top of its real ones (the parameterless
    ///     ctor ran, added a bogus child, and then EF's Include fixup appended
    ///     the real rows into the same list instead of replacing it).
    /// </summary>
    public PaymentPlan()
    {
    }

    /// <summary>
    ///     Schedule is the cuota-by-cuota calendar the frontend built (proportional
    ///     split suggested, dates/amounts edited by the cashier) — the sum of
    ///     its amounts is validated against Sale.TotalAmount by the caller
    ///     (PaymentPlanCommandService), not here.
    /// </summary>
    public PaymentPlan(int saleId, int businessId, IReadOnlyList<(DateOnly DueDate, decimal Amount)> schedule)
    {
        SaleId = saleId;
        BusinessId = businessId;
        TotalInstallments = schedule.Count;

        for (var index = 0; index < schedule.Count; index++)
            _installments.Add(new PaymentInstallment(Id, index + 1, schedule[index].DueDate, schedule[index].Amount));
    }

    public int Id { get; }
    public int SaleId { get; private set; }
    public int BusinessId { get; private set; }
    public int TotalInstallments { get; private set; }
    public int PaidInstallments { get; private set; }

    /// <summary>
    ///     The audit trail behind PaidInstallments — every payment ever
    ///     registered against this plan, reversed ones included. Reversed
    ///     payments stay in this list (see InstallmentPayment.IsReversed);
    ///     they just stop counting toward PaidInstallments/revenue.
    /// </summary>
    public IReadOnlyCollection<InstallmentPayment> Payments => _payments.AsReadOnly();

    /// <summary>The cuota-by-cuota calendar this plan was created with — see PaymentInstallment.</summary>
    public IReadOnlyCollection<PaymentInstallment> Installments => _installments.AsReadOnly();

    /// <summary>
    ///     Set when the sale this plan belongs to gets cancelled — the plan
    ///     itself is never deleted (it stays as a record of what was owed),
    ///     it just stops counting as pending and stops accepting payments.
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    ///     Optimistic-concurrency token — keeps two payments registered at the
    ///     same instant from both counting against the same installment.
    ///     See <see cref="IVersionedEntity" />.
    /// </summary>
    public int Version { get; set; }

    public bool IsFullyPaid => PaidInstallments >= TotalInstallments;

    /// <summary>
    ///     Pays the earliest unpaid cuota by DueDate (never an arbitrary one —
    ///     see PaymentPlanCommandService for the "pay ahead of schedule"
    ///     confirmation the frontend shows before calling this). Caller is
    ///     responsible for rejecting this when already fully paid or cancelled.
    /// </summary>
    public PaymentPlan RegisterPayment(int paidByUserId)
    {
        var nextInstallment = _installments.Where(installment => !installment.IsPaid)
            .OrderBy(installment => installment.DueDate)
            .ThenBy(installment => installment.Number)
            .First();

        nextInstallment.MarkPaid();
        PaidInstallments++;
        _payments.Add(new InstallmentPayment(Id, nextInstallment.Id, nextInstallment.Amount, paidByUserId));
        return this;
    }

    /// <summary>
    ///     Undoes the most recent unreversed payment — a double-click at the
    ///     till, or one registered against the wrong plan. Caller
    ///     (PaymentPlanCommandService) is responsible for rejecting this when
    ///     there is nothing left to revert.
    /// </summary>
    public PaymentPlan RevertLastPayment(int reversedByUserId)
    {
        var lastPayment = _payments.Where(payment => !payment.IsReversed)
            .OrderByDescending(payment => payment.PaidAt)
            .ThenByDescending(payment => payment.Id)
            .First();
        lastPayment.Reverse(reversedByUserId);

        var paidInstallment = _installments.FirstOrDefault(installment => installment.Id == lastPayment.PaymentInstallmentId);
        paidInstallment?.MarkUnpaid();

        PaidInstallments--;
        return this;
    }

    /// <summary>Whether RevertLastPayment has anything to act on.</summary>
    public bool HasReversiblePayment => _payments.Any(payment => !payment.IsReversed);

    /// <summary>
    ///     The next cuota RegisterPayment would pay, if any — used by the
    ///     frontend to warn the cashier before paying a cuota that isn't due
    ///     yet (X6 #7, decision 2). Null once fully paid.
    /// </summary>
    public PaymentInstallment? NextUnpaidInstallment => _installments.Where(installment => !installment.IsPaid)
        .OrderBy(installment => installment.DueDate)
        .ThenBy(installment => installment.Number)
        .FirstOrDefault();

    /// <summary>
    ///     Edits an unpaid cuota's date/amount — allowed even when other
    ///     cuotas in this plan are already paid. Caller (PaymentPlanCommandService)
    ///     is responsible for rejecting an unknown/already-paid installment id
    ///     and for re-validating the plan's total against Sale.TotalAmount.
    /// </summary>
    public PaymentInstallment? FindInstallment(int installmentId)
    {
        return _installments.FirstOrDefault(installment => installment.Id == installmentId);
    }

    /// <summary>Caller (SaleCommandService, on sale cancellation) is responsible for not calling this twice.</summary>
    public PaymentPlan Cancel()
    {
        IsCancelled = true;
        return this;
    }
}
