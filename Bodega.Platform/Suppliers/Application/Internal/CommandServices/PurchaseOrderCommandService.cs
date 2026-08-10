using Cortex.Mediator;
using Microsoft.Extensions.Localization;
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
    IStringLocalizer<SuppliersMessages> localizer)
    : IPurchaseOrderCommandService
{
    public async Task<Result<PurchaseOrder>> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result<PurchaseOrder>.Failure(SuppliersError.EmptyPurchaseOrderLines,
                localizer[nameof(SuppliersError.EmptyPurchaseOrderLines)]);

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
            purchaseOrder.MarkReceived(DateOnly.FromDateTime(DateTime.UtcNow));
            purchaseOrderRepository.Update(purchaseOrder);
            await unitOfWork.CompleteAsync(cancellationToken);

            foreach (var line in purchaseOrder.Details)
            {
                var note = $"Orden de compra #{purchaseOrder.Id}";
                await productContextFacade.RegisterStockIntake(line.ProductId, purchaseOrder.BusinessId, line.Quantity,
                    line.UnitPrice, supplierName, note, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var lines = purchaseOrder.Details.Select(detail => (detail.ProductId, detail.Quantity)).ToList();
            await mediator.PublishAsync(
                new PurchaseOrderReceivedEvent(purchaseOrder.Id, purchaseOrder.BusinessId, supplierName, lines),
                cancellationToken);

            return Result<PurchaseOrder>.Success(purchaseOrder);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<PurchaseOrder>.Failure(SuppliersError.DatabaseError, localizer[nameof(SuppliersError.DatabaseError)]);
        }
    }
}
