using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Sales.Application.CommandServices;
using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Domain.Model.Entities;
using Bodega.Platform.Sales.Domain.Model.Errors;
using Bodega.Platform.Sales.Domain.Repositories;
using Bodega.Platform.Sales.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Sales.Application.Internal.CommandServices;

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
        var plan = await paymentPlanRepository.FindByIdAsync(command.PaymentPlanId, cancellationToken);
        if (plan == null)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanNotFound,
                localizer[nameof(SalesError.PaymentPlanNotFound)]);

        if (plan.IsCancelled)
            return Result<PaymentPlan>.Failure(SalesError.PaymentPlanCancelled,
                localizer[nameof(SalesError.PaymentPlanCancelled)]);

        if (plan.IsFullyPaid)
            return Result<PaymentPlan>.Failure(SalesError.InstallmentsFullyPaid,
                localizer[nameof(SalesError.InstallmentsFullyPaid)]);

        plan.RegisterPayment();
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
}
