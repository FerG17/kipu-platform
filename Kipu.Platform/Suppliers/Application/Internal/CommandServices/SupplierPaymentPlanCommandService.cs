using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Suppliers.Application.CommandServices;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Domain.Model.Errors;
using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Suppliers.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Application.Internal.CommandServices;

/// <summary>
///     Attaches/updates a credit payment plan on an existing PurchaseOrder
///     (X6 #12) — mirrors Sales' PaymentPlanCommandService (X6 #7) exactly.
///     Kept entirely separate from PurchaseOrderCommandService on purpose: a
///     payment plan never touches how the order was created, totaled, or
///     received.
/// </summary>
public class SupplierPaymentPlanCommandService(
    ISupplierPaymentPlanRepository supplierPaymentPlanRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateSupplierPaymentPlanCommand> createSupplierPaymentPlanValidator,
    IValidator<UpdateSupplierPaymentInstallmentCommand> updateSupplierPaymentInstallmentValidator,
    IStringLocalizer<SuppliersMessages> localizer)
    : ISupplierPaymentPlanCommandService
{
    public async Task<Result<SupplierPaymentPlan>> Handle(CreateSupplierPaymentPlanCommand command, CancellationToken cancellationToken)
    {
        if (!(await createSupplierPaymentPlanValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.InvalidSupplierInstallmentSchedule,
                localizer[nameof(SuppliersError.InvalidSupplierInstallmentSchedule)]);

        var purchaseOrder = await purchaseOrderRepository.FindByIdWithDetailsAsync(command.PurchaseOrderId, cancellationToken);
        if (purchaseOrder == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.PurchaseOrderNotFound,
                localizer[nameof(SuppliersError.PurchaseOrderNotFound)]);

        // A credit purchase can be attached whether the order is still
        // PENDING, already RECEIVED, or DELAYED (decision 12, point 5 — the
        // debt is independent of whether the goods arrived) — only a
        // CANCELLED order rejects it, same reasoning as a cancelled sale.
        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.PurchaseOrderCancelled,
                localizer[nameof(SuppliersError.PurchaseOrderCancelled)]);

        if (await supplierPaymentPlanRepository.FindByPurchaseOrderIdAsync(command.PurchaseOrderId, cancellationToken) != null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanAlreadyExists,
                localizer[nameof(SuppliersError.SupplierPaymentPlanAlreadyExists)]);

        // The purchaser enters every cuota's date and amount by hand (X6
        // #12) — the one thing never left to trust from the client is
        // whether they add up. No margin: either it matches the order's
        // total to the cent, or the plan is rejected outright.
        var orderTotal = purchaseOrder.Details.Sum(detail => detail.Subtotal);
        if (command.Schedule.Sum(line => line.Amount) != orderTotal)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierInstallmentAmountMismatch,
                localizer[nameof(SuppliersError.SupplierInstallmentAmountMismatch)]);

        var schedule = command.Schedule.Select(line => (line.DueDate, line.Amount)).ToList();
        var plan = new SupplierPaymentPlan(command.PurchaseOrderId, command.BusinessId, schedule);
        await supplierPaymentPlanRepository.AddAsync(plan, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<SupplierPaymentPlan>.Success(plan);
    }

    public async Task<Result<SupplierPaymentPlan>> Handle(RegisterSupplierInstallmentPaymentCommand command, CancellationToken cancellationToken)
    {
        var plan = await supplierPaymentPlanRepository.FindByIdWithScheduleAsync(command.SupplierPaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanNotFound,
                localizer[nameof(SuppliersError.SupplierPaymentPlanNotFound)]);

        if (plan.IsCancelled)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanCancelled,
                localizer[nameof(SuppliersError.SupplierPaymentPlanCancelled)]);

        if (plan.IsFullyPaid)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierInstallmentsFullyPaid,
                localizer[nameof(SuppliersError.SupplierInstallmentsFullyPaid)]);

        // The amount comes from the earliest unpaid SupplierPaymentInstallment
        // in the plan's own calendar — the purchaser registers "the next
        // cuota", never an arbitrary figure.
        plan.RegisterPayment(command.PaidByUserId);
        supplierPaymentPlanRepository.Update(plan);

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.ConcurrentModification,
                localizer[nameof(SuppliersError.ConcurrentModification)]);
        }

        return Result<SupplierPaymentPlan>.Success(plan);
    }

    /// <summary>Undoes the most recently registered payment on a plan — reverses the payment record rather than deleting it.</summary>
    public async Task<Result<SupplierPaymentPlan>> Handle(RevertSupplierInstallmentPaymentCommand command, CancellationToken cancellationToken)
    {
        var plan = await supplierPaymentPlanRepository.FindByIdWithScheduleAsync(command.SupplierPaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanNotFound,
                localizer[nameof(SuppliersError.SupplierPaymentPlanNotFound)]);

        if (!plan.HasReversiblePayment)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.NoSupplierPaymentToRevert,
                localizer[nameof(SuppliersError.NoSupplierPaymentToRevert)]);

        plan.RevertLastPayment(command.RevertedByUserId);
        supplierPaymentPlanRepository.Update(plan);

        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.ConcurrentModification,
                localizer[nameof(SuppliersError.ConcurrentModification)]);
        }

        return Result<SupplierPaymentPlan>.Success(plan);
    }

    /// <summary>
    ///     Edits an unpaid cuota's date/amount — allowed even when other
    ///     cuotas in the same plan are already paid. The resulting schedule
    ///     must still add up exactly to the purchase order's total.
    /// </summary>
    public async Task<Result<SupplierPaymentPlan>> Handle(UpdateSupplierPaymentInstallmentCommand command, CancellationToken cancellationToken)
    {
        if (!(await updateSupplierPaymentInstallmentValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.InvalidSupplierInstallmentSchedule,
                localizer[nameof(SuppliersError.InvalidSupplierInstallmentSchedule)]);

        var plan = await supplierPaymentPlanRepository.FindByIdWithScheduleAsync(command.SupplierPaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanNotFound,
                localizer[nameof(SuppliersError.SupplierPaymentPlanNotFound)]);

        if (plan.IsCancelled)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierPaymentPlanCancelled,
                localizer[nameof(SuppliersError.SupplierPaymentPlanCancelled)]);

        var installment = plan.FindInstallment(command.InstallmentId);
        if (installment == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierInstallmentNotFound,
                localizer[nameof(SuppliersError.SupplierInstallmentNotFound)]);

        if (installment.IsPaid)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierInstallmentAlreadyPaid,
                localizer[nameof(SuppliersError.SupplierInstallmentAlreadyPaid)]);

        var purchaseOrder = await purchaseOrderRepository.FindByIdWithDetailsAsync(plan.PurchaseOrderId, cancellationToken);
        if (purchaseOrder == null)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.PurchaseOrderNotFound,
                localizer[nameof(SuppliersError.PurchaseOrderNotFound)]);

        var orderTotal = purchaseOrder.Details.Sum(detail => detail.Subtotal);
        var prospectiveTotal = plan.Installments.Where(other => other.Id != installment.Id).Sum(other => other.Amount)
                                + command.Amount;
        if (prospectiveTotal != orderTotal)
            return Result<SupplierPaymentPlan>.Failure(SuppliersError.SupplierInstallmentAmountMismatch,
                localizer[nameof(SuppliersError.SupplierInstallmentAmountMismatch)]);

        installment.UpdateSchedule(command.DueDate, command.Amount);
        supplierPaymentPlanRepository.Update(plan);
        await unitOfWork.CompleteAsync(cancellationToken);

        return Result<SupplierPaymentPlan>.Success(plan);
    }
}
