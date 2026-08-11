using Cortex.Mediator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Suppliers.Application.CommandServices;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Domain.Model.Errors;
using Bodega.Platform.Suppliers.Domain.Model.Events;
using Bodega.Platform.Suppliers.Domain.Repositories;
using Bodega.Platform.Suppliers.Resources;

namespace Bodega.Platform.Suppliers.Application.Internal.CommandServices;

public class PurchaseOrderCommandService(
    IPurchaseOrderRepository purchaseOrderRepository,
    ISupplierRepository supplierRepository,
    IProductContextFacade productContextFacade,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IValidator<CreatePurchaseOrderCommand> createPurchaseOrderValidator,
    IStringLocalizer<SuppliersMessages> localizer,
    IBusinessClock businessClock)
    : IPurchaseOrderCommandService
{
    public async Task<Result<PurchaseOrder>> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result<PurchaseOrder>.Failure(SuppliersError.EmptyPurchaseOrderLines,
                localizer[nameof(SuppliersError.EmptyPurchaseOrderLines)]);

        // Quantities and money on a purchase line were never checked at all —
        // see CreatePurchaseOrderCommandValidator.
        if (!(await createPurchaseOrderValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<PurchaseOrder>.Failure(SuppliersError.InvalidPurchaseOrderLine,
                localizer[nameof(SuppliersError.InvalidPurchaseOrderLine)]);

        var supplier = await supplierRepository.FindByIdAsync(command.SupplierId, cancellationToken);
        if (supplier == null)
            return Result<PurchaseOrder>.Failure(SuppliersError.SupplierNotFound, localizer[nameof(SuppliersError.SupplierNotFound)]);

        foreach (var line in command.Lines)
        {
            if (!await productContextFacade.ProductExists(line.ProductId, cancellationToken))
                return Result<PurchaseOrder>.Failure(SuppliersError.ProductNotFound, localizer[nameof(SuppliersError.ProductNotFound)]);
        }

        var purchaseOrder = new PurchaseOrder(command.BusinessId, command.SupplierId, command.Date, command.ExpectedDate,
            command.Currency, command.Description);
        foreach (var line in command.Lines)
            purchaseOrder.AddLine(line.ProductId, line.Quantity, line.UnitPrice, line.Discount);

        await purchaseOrderRepository.AddAsync(purchaseOrder, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<PurchaseOrder>.Success(purchaseOrder);
    }

    /// <summary>
    ///     Moving to RECEIVED triggers a real stock intake per line, tagged
    ///     with the supplier + a note referencing the order — distinct from a
    ///     manual inventory intake (see architecture doc §6.6). Wrapped in one
    ///     transaction so the status change and every line's stock intake
    ///     succeed or fail together.
    /// </summary>
    public async Task<Result<PurchaseOrder>> Handle(UpdatePurchaseOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var purchaseOrder = await purchaseOrderRepository.FindByIdWithDetailsAsync(command.PurchaseOrderId, cancellationToken);
        if (purchaseOrder == null)
            return Result<PurchaseOrder>.Failure(SuppliersError.PurchaseOrderNotFound,
                localizer[nameof(SuppliersError.PurchaseOrderNotFound)]);

        // A received or cancelled order is final. Without this guard, a second
        // "RECEIVED" — a double click is enough — ran the stock intake again
        // and booked the same delivery into inventory twice, leaving
        // duplicate StockMovements in the entradas/salidas report.
        if (purchaseOrder.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            return Result<PurchaseOrder>.Failure(SuppliersError.InvalidStatusTransition,
                localizer[nameof(SuppliersError.InvalidStatusTransition)]);

        switch (command.Status)
        {
            case PurchaseOrderStatus.Received:
                return await MarkReceived(purchaseOrder, cancellationToken);
            case PurchaseOrderStatus.Delayed:
                purchaseOrder.MarkDelayed();
                break;
            case PurchaseOrderStatus.Cancelled:
                purchaseOrder.Cancel();
                break;
            default:
                return Result<PurchaseOrder>.Failure(SuppliersError.InvalidStatusTransition,
                    localizer[nameof(SuppliersError.InvalidStatusTransition)]);
        }

        purchaseOrderRepository.Update(purchaseOrder);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<PurchaseOrder>.Success(purchaseOrder);
    }

    private async Task<Result<PurchaseOrder>> MarkReceived(PurchaseOrder purchaseOrder, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.FindByIdAsync(purchaseOrder.SupplierId, cancellationToken);
        var supplierName = supplier != null ? $"{supplier.Name} {supplier.LastName}".Trim() : string.Empty;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            purchaseOrder.MarkReceived(businessClock.Today);
            purchaseOrderRepository.Update(purchaseOrder);
            await unitOfWork.CompleteAsync(cancellationToken);

            foreach (var line in purchaseOrder.Details)
            {
                var note = $"Orden de compra #{purchaseOrder.Id}";
                var stocked = await productContextFacade.RegisterStockIntake(line.ProductId, purchaseOrder.BusinessId,
                    line.Quantity, line.UnitPrice, supplierName, note, purchaseOrder.SupplierId, cancellationToken);
                if (stocked) continue;

                // RECEIVED is a promise that the goods are on the shelf. This
                // used to be a fire-and-forget call whose outcome was thrown
                // away, so an intake that failed (no warehouse to receive
                // into, a product since removed) still left the order marked
                // RECEIVED with nothing added to inventory — the mirror of the
                // swallowed DecrementStock this codebase already fixed on the
                // sales side. The order only moves if its stock really did.
                await transaction.RollbackAsync(cancellationToken);
                return Result<PurchaseOrder>.Failure(SuppliersError.DatabaseError,
                    localizer[nameof(SuppliersError.DatabaseError)]);
            }

            await transaction.CommitAsync(cancellationToken);

            var lines = purchaseOrder.Details.Select(detail => (detail.ProductId, detail.Quantity)).ToList();
            await mediator.PublishAsync(
                new PurchaseOrderReceivedEvent(purchaseOrder.Id, purchaseOrder.BusinessId, supplierName, lines),
                cancellationToken);

            return Result<PurchaseOrder>.Success(purchaseOrder);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request already moved this order to RECEIVED. The status
            // guard above is a read, so several simultaneous submits all
            // passed it and each booked the same delivery: twenty units
            // arrived as one hundred and sixty. The concurrency token makes
            // only the first write land.
            await transaction.RollbackAsync(cancellationToken);
            return Result<PurchaseOrder>.Failure(SuppliersError.InvalidStatusTransition,
                localizer[nameof(SuppliersError.InvalidStatusTransition)]);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<PurchaseOrder>.Failure(SuppliersError.DatabaseError, localizer[nameof(SuppliersError.DatabaseError)]);
        }
    }
}
