using Cortex.Mediator;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Sales.Application.CommandServices;
using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Domain.Model.Errors;
using Bodega.Platform.Sales.Domain.Model.Events;
using Bodega.Platform.Sales.Domain.Repositories;
using Bodega.Platform.Sales.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Sales.Application.Internal.CommandServices;

public class SaleCommandService(
    ISaleRepository saleRepository,
    ICustomerRepository customerRepository,
    IProductContextFacade productContextFacade,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IStringLocalizer<SalesMessages> localizer)
    : ISaleCommandService
{
    /// <summary>
    ///     Validates every line has sufficient stock BEFORE writing anything
    ///     (rejects the whole sale if any line fails — "todo o nada"), then
    ///     persists the sale and decrements stock for every line inside one
    ///     explicit transaction, so a failure partway through never leaves a
    ///     sale registered without its stock actually decremented.
    /// </summary>
    public async Task<Result<Sale>> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result<Sale>.Failure(SalesError.EmptySaleLines, localizer[nameof(SalesError.EmptySaleLines)]);

        if (command.CustomerId.HasValue)
        {
            var customer = await customerRepository.FindByIdAsync(command.CustomerId.Value, cancellationToken);
            if (customer == null || customer.BusinessId != command.BusinessId)
                return Result<Sale>.Failure(SalesError.CustomerNotFound, localizer[nameof(SalesError.CustomerNotFound)]);
        }

        foreach (var line in command.Lines)
        {
            // Explicit existence check first (not just relying on the stock
            // check below): a quantity-0 line for a product outside this
            // business would otherwise pass "stock >= 0" silently and let a
            // Sale reference a ProductId it doesn't own.
            if (!await productContextFacade.ProductExists(line.ProductId, cancellationToken))
                return Result<Sale>.Failure(SalesError.ProductNotFound, localizer[nameof(SalesError.ProductNotFound)]);

            var availableStock = await productContextFacade.GetAvailableStock(line.ProductId, cancellationToken);
            if (availableStock < line.Quantity)
                return Result<Sale>.Failure(SalesError.InsufficientStock, localizer[nameof(SalesError.InsufficientStock)]);
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var sale = new Sale(command.BusinessId, command.CustomerId, command.PaymentMethod, command.Currency,
                command.Description);
            foreach (var line in command.Lines)
                sale.AddLine(line.ProductId, line.Quantity, line.UnitPrice, line.Discount);

            await saleRepository.AddAsync(sale, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            foreach (var line in command.Lines)
                await productContextFacade.DecrementStock(line.ProductId, command.BusinessId, line.Quantity, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var lines = command.Lines.Select(line => (line.ProductId, line.Quantity)).ToList();
            await mediator.PublishAsync(new SaleRegisteredEvent(sale.Id, sale.BusinessId, lines), cancellationToken);

            return Result<Sale>.Success(sale);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Sale>.Failure(SalesError.DatabaseError, localizer[nameof(SalesError.DatabaseError)]);
        }
    }

    public async Task<Result<Sale>> Handle(UpdateSaleStatusCommand command, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.FindByIdAsync(command.SaleId, cancellationToken);
        if (sale == null) return Result<Sale>.Failure(SalesError.SaleNotFound, localizer[nameof(SalesError.SaleNotFound)]);

        if (sale.Status == SaleStatus.Cancelled)
            return Result<Sale>.Failure(SalesError.SaleAlreadyCancelled, localizer[nameof(SalesError.SaleAlreadyCancelled)]);

        sale.Cancel();
        saleRepository.Update(sale);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Sale>.Success(sale);
    }
}
