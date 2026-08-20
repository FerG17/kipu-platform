using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Sales.Application.CommandServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Model.Errors;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Sales.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.CommandServices;

/// <summary>
///     Attaches/updates a credit payment plan on an existing Sale. Kept
///     entirely separate from SaleCommandService on purpose — the brief was
///     explicit about not touching how a Sale is created, totaled, or how
///     stock gets decremented; a payment plan is layered on afterward via
///     its own command, never part of CreateSaleCommand.
/// </summary>
public class PaymentPlanCommandService(
    IPaymentPlanRepository paymentPlanRepository,
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreatePaymentPlanCommand> createPaymentPlanValidator,
    IStringLocalizer<SalesMessages> localizer)
    : IPaymentPlanCommandService
{
    public async Task<Result<PaymentPlan>> Handle(CreatePaymentPlanCommand command, CancellationToken cancellationToken)
    {
        if (!(await createPaymentPlanValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<PaymentPlan>.Failure(SalesError.InvalidInstallmentCount,
                localizer[nameof(SalesError.InvalidInstallmentCount)]);

        var sale = await saleRepository.FindByIdAsync(command.SaleId, cancellationToken);
        if (sale == null)
            return Result<PaymentPlan>.Failure(SalesError.SaleNotFound, localizer[nameof(SalesError.SaleNotFound)]);

        if (sale.Status == SaleStatus.Cancelled)
            return Result<PaymentPlan>.Failure(SalesError.SaleAlreadyCancelled,
                localizer[nameof(SalesError.SaleAlreadyCancelled)]);

        // A plan can only exist against a sale actually checked out on
        // credit (see Sale.Status/SalePaymentMethod.Credit) — otherwise revenue
        // for an already-Paid sale would be double counted the moment a plan
        // got attached to it (SalesContextFacade sums Paid totals AND
        // collected installments; a Paid sale was never meant to have either).
        if (sale.Status != SaleStatus.Credit)
            return Result<PaymentPlan>.Failure(SalesError.SaleIsNotACreditSale,
                localizer[nameof(SalesError.SaleIsNotACreditSale)]);

        if (await paymentPlanRepository.FindBySaleIdAsync(command.SaleId, cancellationToken) != null)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanAlreadyExists,
                localizer[nameof(SalesError.PaymentPlanAlreadyExists)]);

        var plan = new PaymentPlan(command.SaleId, command.BusinessId, command.TotalInstallments);
        await paymentPlanRepository.AddAsync(plan, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<PaymentPlan>.Success(plan);
    }

    public async Task<Result<PaymentPlan>> Handle(RegisterInstallmentPaymentCommand command, CancellationToken cancellationToken)
    {
        var plan = await paymentPlanRepository.FindByIdWithPaymentsAsync(command.PaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanNotFound,
                localizer[nameof(SalesError.PaymentPlanNotFound)]);

        if (plan.IsCancelled)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanCancelled,
                localizer[nameof(SalesError.PaymentPlanCancelled)]);

        if (plan.IsFullyPaid)
            return Result<PaymentPlan>.Failure(SalesError.InstallmentsFullyPaid,
                localizer[nameof(SalesError.InstallmentsFullyPaid)]);

        // The amount is always Sale.TotalAmount / TotalInstallments, computed
        // here rather than taken from the caller — the cashier registers
        // "the next installment", not an arbitrary figure, so there is
        // nothing to trust from client input. Standard installments round to
        // the cent; whatever that rounding drops or adds is folded into the
        // LAST installment instead of silently drifting the plan's total
        // away from the sale's real TotalAmount by a cent or two.
        var sale = await saleRepository.FindByIdAsync(plan.SaleId, cancellationToken);
        if (sale == null)
            return Result<PaymentPlan>.Failure(SalesError.SaleNotFound, localizer[nameof(SalesError.SaleNotFound)]);

        var standardInstallmentAmount = Math.Round(sale.TotalAmount / plan.TotalInstallments, 2, MidpointRounding.AwayFromZero);
        var isLastInstallment = plan.PaidInstallments == plan.TotalInstallments - 1;
        var amount = isLastInstallment
            ? sale.TotalAmount - standardInstallmentAmount * (plan.TotalInstallments - 1)
            : standardInstallmentAmount;

        plan.RegisterPayment(amount, command.PaidByUserId);
        paymentPlanRepository.Update(plan);

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // "Is it already fully paid?" above is a read, so two payments
            // registered at the same instant both passed it and both counted.
            // PaymentPlan's concurrency token lets only one of them land; the
            // other is told to look at the plan again.
            return Result<PaymentPlan>.Failure(SalesError.ConcurrentModification,
                localizer[nameof(SalesError.ConcurrentModification)]);
        }

        return Result<PaymentPlan>.Success(plan);
    }

    /// <summary>
    ///     Undoes the most recently registered payment on a plan — a
    ///     double-click at the till, or one registered against the wrong
    ///     plan. Reverses the payment record rather than deleting it (see
    ///     InstallmentPayment.Reverse), so the mistake and its correction
    ///     both stay in the trail.
    /// </summary>
    public async Task<Result<PaymentPlan>> Handle(RevertInstallmentPaymentCommand command, CancellationToken cancellationToken)
    {
        var plan = await paymentPlanRepository.FindByIdWithPaymentsAsync(command.PaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanNotFound,
                localizer[nameof(SalesError.PaymentPlanNotFound)]);

        if (!plan.HasReversiblePayment)
            return Result<PaymentPlan>.Failure(SalesError.NoPaymentToRevert,
                localizer[nameof(SalesError.NoPaymentToRevert)]);

        plan.RevertLastPayment(command.RevertedByUserId);
        paymentPlanRepository.Update(plan);

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Same reasoning as RegisterInstallmentPaymentCommand above — a
            // register/revert landed on this plan first; look at it again
            // rather than reverting a payment that isn't the one intended.
            return Result<PaymentPlan>.Failure(SalesError.ConcurrentModification,
                localizer[nameof(SalesError.ConcurrentModification)]);
        }

        return Result<PaymentPlan>.Success(plan);
    }
}
