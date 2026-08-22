using Kipu.Platform.Shared.Domain.Model.Entities;

namespace Kipu.Platform.Sales.Domain.Model.Entities;

/// <summary>
///     Credit sales tracking, kept deliberately simple per the brief: just
///     how many installments a sale is split into, and how many have been
///     paid — no due dates, no per-installment amounts, no changes to how a
///     Sale itself is created/totaled/decremented (see CreateSaleCommand,
///     untouched). Attached to an existing Sale after the fact via a
///     separate command, at most one plan per sale.
/// </summary>
public class PaymentPlan(int saleId, int businessId, int totalInstallments) : IVersionedEntity
{
    private readonly List<InstallmentPayment> _payments = [];

    public PaymentPlan() : this(0, 0, 1)
    {
    }

    public int Id { get; }
    public int SaleId { get; private set; } = saleId;
    public int BusinessId { get; private set; } = businessId;
    public int TotalInstallments { get; private set; } = totalInstallments;
    public int PaidInstallments { get; private set; }

    /// <summary>
    ///     The audit trail behind PaidInstallments — every payment ever
    ///     registered against this plan, reversed ones included. Reversed
    ///     payments stay in this list (see InstallmentPayment.IsReversed);
    ///     they just stop counting toward PaidInstallments/revenue.
    /// </summary>
    public IReadOnlyCollection<InstallmentPayment> Payments => _payments.AsReadOnly();

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
    ///     Caller (PaymentPlanCommandService) is responsible for rejecting
    ///     this when already fully paid or cancelled, and for computing
    ///     `amount` (Sale.TotalAmount / TotalInstallments, remainder folded
    ///     into the last one) — this method just records whatever it's given.
    /// </summary>
    public PaymentPlan RegisterPayment(decimal amount, int paidByUserId)
    {
        PaidInstallments++;
        _payments.Add(new InstallmentPayment(Id, amount, paidByUserId));
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
        PaidInstallments--;
        return this;
    }

    /// <summary>Whether RevertLastPayment has anything to act on.</summary>
    public bool HasReversiblePayment => _payments.Any(payment => !payment.IsReversed);

    /// <summary>Caller (SaleCommandService, on sale cancellation) is responsible for not calling this twice.</summary>
    public PaymentPlan Cancel()
    {
        IsCancelled = true;
        return this;
    }
}
