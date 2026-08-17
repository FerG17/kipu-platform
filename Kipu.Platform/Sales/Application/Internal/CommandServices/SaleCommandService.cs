using Cortex.Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Sales.Application.CommandServices;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Domain.Model.Errors;
using Kipu.Platform.Sales.Domain.Model.Events;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Sales.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Application.Internal.CommandServices;

public class SaleCommandService(
    ISaleRepository saleRepository,
    ICustomerRepository customerRepository,
    IPaymentPlanRepository paymentPlanRepository,
    IProductContextFacade productContextFacade,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IValidator<CreateSaleCommand> createSaleValidator,
    IStringLocalizer<SalesMessages> localizer,
    ILogger<SaleCommandService> logger)
    : ISaleCommandService
{
    /// <summary>
    ///     Validates every product has sufficient stock BEFORE writing
    ///     anything (rejects the whole sale if any fails — "todo o nada"),
    ///     then persists the sale and decrements stock inside one explicit
    ///     transaction, aborting the whole thing if any decrement fails, so a
    ///     sale is never registered without its stock actually coming off.
    /// </summary>
    public async Task<Result<Sale>> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result<Sale>.Failure(SalesError.EmptySaleLines, localizer[nameof(SalesError.EmptySaleLines)]);

        if (!(await createSaleValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Sale>.Failure(SalesError.InvalidSaleLine, localizer[nameof(SalesError.InvalidSaleLine)]);

        if (command.CustomerId.HasValue)
        {
            var customer = await customerRepository.FindByIdAsync(command.CustomerId.Value, cancellationToken);
            if (customer == null || customer.BusinessId != command.BusinessId)
                return Result<Sale>.Failure(SalesError.CustomerNotFound, localizer[nameof(SalesError.CustomerNotFound)]);
        }

        // Grouped by product, not per line: a POS that scans the same item
        // twice produces two lines for one product, and checking each line
        // independently against the same total stock lets their combined
        // quantity exceed what actually exists.
        //
        // The admin-set BasePrice is also captured here, per product, and used
        // below instead of whatever UnitPrice the client submitted — the UI
        // never lets a cashier edit it, but the API must not take a client-sent
        // price on faith either.
        var basePriceByProductId = new Dictionary<int, decimal>();
        foreach (var productLines in command.Lines.GroupBy(line => line.ProductId))
        {
            // Explicit existence check first (not just relying on the stock
            // check below): a product outside this business would otherwise
            // be caught only by "stock >= quantity" and report the wrong
            // reason, and could let a Sale reference a ProductId it doesn't own.
            var basePrice = await productContextFacade.GetBasePrice(productLines.Key, cancellationToken);
            if (basePrice is null)
                return Result<Sale>.Failure(SalesError.ProductNotFound, localizer[nameof(SalesError.ProductNotFound)]);
            basePriceByProductId[productLines.Key] = basePrice.Value;

            var requestedQuantity = productLines.Sum(line => line.Quantity);
            var availableStock = await productContextFacade.GetAvailableStock(productLines.Key, cancellationToken);
            if (availableStock < requestedQuantity)
                return Result<Sale>.Failure(SalesError.InsufficientStock, localizer[nameof(SalesError.InsufficientStock)]);
        }

        Sale sale;
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                sale = new Sale(command.BusinessId, command.CustomerId, command.PaymentMethod, command.Currency,
                    command.Description);
                foreach (var line in command.Lines)
                    sale.AddLine(line.ProductId, line.Quantity, basePriceByProductId[line.ProductId], line.Discount);

                await saleRepository.AddAsync(sale, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);

                foreach (var line in command.Lines)
                {
                    // The pre-check above can still be beaten by a concurrent
                    // sale, so this is the authoritative one — the sale is only
                    // real if every unit actually came off inventory.
                    var decremented = await productContextFacade.DecrementStock(line.ProductId, command.BusinessId,
                        line.Quantity, cancellationToken);
                    if (decremented) continue;

                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogWarning(
                        "Sale for business {BusinessId} rolled back: stock for product {ProductId} could not be decremented by {Quantity}",
                        command.BusinessId, line.ProductId, line.Quantity);
                    return Result<Sale>.Failure(SalesError.InsufficientStock,
                        localizer[nameof(SalesError.InsufficientStock)]);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another till took the same units between this request's stock
                // check and its write. The concurrency token on InventoryItem
                // is what surfaces it: without it both writes landed and the
                // shop sold stock it did not have. Insufficient stock is the
                // honest answer to give the cashier, not a 500.
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Sale for business {BusinessId} lost a concurrent race for stock and was rolled back",
                    command.BusinessId);
                return Result<Sale>.Failure(SalesError.InsufficientStock,
                    localizer[nameof(SalesError.InsufficientStock)]);
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(exception, "Failed to register sale for business {BusinessId} ({LineCount} line(s))",
                    command.BusinessId, command.Lines.Count);
                return Result<Sale>.Failure(SalesError.DatabaseError, localizer[nameof(SalesError.DatabaseError)]);
            }
        }

        // Deliberately outside the try/transaction: the sale is already
        // committed at this point, so a failure publishing the event or
        // writing the log must not trigger a rollback of a closed transaction.
        var eventLines = command.Lines.Select(line => (line.ProductId, line.Quantity)).ToList();
        await mediator.PublishAsync(new SaleRegisteredEvent(sale.Id, sale.BusinessId, eventLines), cancellationToken);

        logger.LogInformation(
            "Sale {SaleId} registered for business {BusinessId}: {LineCount} line(s), total {TotalAmount} {Currency}, payment method {PaymentMethod}",
            sale.Id, sale.BusinessId, command.Lines.Count, sale.TotalAmount, sale.Currency, sale.PaymentMethod);

        return Result<Sale>.Success(sale);
    }

    /// <summary>
    ///     Cancelling means the goods came back, so the stock comes back with
    ///     them — the sale's lines are returned to inventory in the same
    ///     transaction as the status change. Before this, cancelling only
    ///     flipped the status: the units stayed deducted forever while the
    ///     revenue disappeared, so inventory drifted below reality with every
    ///     cancellation and nothing ever reconciled it.
    /// </summary>
    public async Task<Result<Sale>> Handle(UpdateSaleStatusCommand command, CancellationToken cancellationToken)
    {
        // With details: the lines are what has to go back on the shelf, and
        // the plain FindByIdAsync does not load them.
        var sale = await saleRepository.FindByIdWithDetailsAsync(command.SaleId, cancellationToken);
        if (sale == null) return Result<Sale>.Failure(SalesError.SaleNotFound, localizer[nameof(SalesError.SaleNotFound)]);

        // The requested status used to be ignored entirely: every call fell
        // through to Cancel(), so asking to mark a sale PAID silently
        // cancelled it and answered 200. Cancelling is still the only
        // transition with any meaning (a sale is born PAID), but now the API
        // says so instead of quietly doing something else.
        if (command.Status != SaleStatus.Cancelled)
            return Result<Sale>.Failure(SalesError.InvalidStatusTransition,
                localizer[nameof(SalesError.InvalidStatusTransition)]);

        if (sale.Status == SaleStatus.Cancelled)
            return Result<Sale>.Failure(SalesError.SaleAlreadyCancelled, localizer[nameof(SalesError.SaleAlreadyCancelled)]);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            sale.Cancel();
            saleRepository.Update(sale);
            await unitOfWork.CompleteAsync(cancellationToken);

            foreach (var line in sale.SaleDetails)
            {
                var restored = await productContextFacade.RestoreStock(line.ProductId, sale.BusinessId, line.Quantity,
                    cancellationToken);
                if (restored) continue;

                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning(
                    "Cancellation of sale {SaleId} rolled back: stock for product {ProductId} could not be restored",
                    sale.Id, line.ProductId);
                return Result<Sale>.Failure(SalesError.DatabaseError, localizer[nameof(SalesError.DatabaseError)]);
            }

            // A credit sale being cancelled means the debt it created is gone
            // too — otherwise the plan keeps showing as pending against a
            // sale that no longer exists, and can still take payments.
            var paymentPlan = await paymentPlanRepository.FindBySaleIdAsync(sale.Id, cancellationToken);
            if (paymentPlan != null)
            {
                paymentPlan.Cancel();
                paymentPlanRepository.Update(paymentPlan);
                await unitOfWork.CompleteAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The same sale was cancelled by a request that got there first.
            // Without the concurrency token every simultaneous cancellation
            // committed, and each one handed the sale's units back to stock —
            // eight clicks on "cancelar" turned four units into thirty-two.
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning("Cancellation of sale {SaleId} lost a concurrent race and was rolled back", sale.Id);
            return Result<Sale>.Failure(SalesError.SaleAlreadyCancelled,
                localizer[nameof(SalesError.SaleAlreadyCancelled)]);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(exception, "Failed to cancel sale {SaleId} for business {BusinessId}", sale.Id, sale.BusinessId);
            return Result<Sale>.Failure(SalesError.DatabaseError, localizer[nameof(SalesError.DatabaseError)]);
        }

        logger.LogInformation("Sale {SaleId} cancelled for business {BusinessId}: {LineCount} line(s) returned to stock",
            sale.Id, sale.BusinessId, sale.SaleDetails.Count);

        return Result<Sale>.Success(sale);
    }
}
