using Bodega.Platform.Alerts.Application.Internal.OutboundServices;
using Bodega.Platform.Alerts.Domain.Model.Aggregates;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Products.Domain.Model.Events;
using Bodega.Platform.Shared.Application.Internal.EventHandlers;
using Bodega.Platform.Shared.Domain.Model.Services;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>
///     Reactive half of the alerts engine (§5.4): re-evaluates low-stock /
///     out-of-stock for the exact InventoryItem that just changed —
///     covers the 90% of cases where a concrete action (sale, intake,
///     adjustment) is what changed the stock.
///
///     Scoped per (product, warehouse): a product split across warehouses
///     can be critically low in one and perfectly healthy in another, so
///     each warehouse gets its own independent alert instead of one
///     ambiguous "the product is low" flag that a healthy warehouse's event
///     could refresh/hide.
/// </summary>
public class StockLevelChangedEventHandler(
    IAlertRepository alertRepository,
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IAlertNotificationDispatcher notificationDispatcher)
    : IEventHandler<StockLevelChangedEvent>
{
    public async Task Handle(StockLevelChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        var outOfStockRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(domainEvent.BusinessId,
            AlertType.OutOfStock, cancellationToken);
        var lowStockRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(domainEvent.BusinessId,
            AlertType.LowStock, cancellationToken);
        var outOfStockEnabled = outOfStockRule?.Enabled ?? true;
        var lowStockEnabled = lowStockRule?.Enabled ?? true;

        var existingLowStock = await alertRepository.FindActiveByProductAndTypeAsync(domainEvent.ProductId, AlertType.LowStock,
            null, domainEvent.WarehouseId, cancellationToken);
        var existingOutOfStock = await alertRepository.FindActiveByProductAndTypeAsync(domainEvent.ProductId,
            AlertType.OutOfStock, null, domainEvent.WarehouseId, cancellationToken);

        var isOutOfStock = StockRules.IsOutOfStock(domainEvent.NewQuantity);
        var isLowStock = StockRules.IsLowStock(domainEvent.NewQuantity, domainEvent.MinimumStock);

        Alert? newAlert = null;

        if (isOutOfStock && outOfStockEnabled)
        {
            var message = $"{domainEvent.ProductName} está agotado.";
            if (existingOutOfStock != null)
            {
                existingOutOfStock.RefreshStockInfo(AlertSeverity.High, message, domainEvent.NewQuantity);
            }
            else
            {
                newAlert = new Alert(domainEvent.BusinessId, domainEvent.ProductId, null, domainEvent.ProductName,
                    AlertType.OutOfStock, AlertSeverity.High, message, domainEvent.NewQuantity, domainEvent.MinimumStock,
                    null, domainEvent.WarehouseId);
                await alertRepository.AddAsync(newAlert, cancellationToken);
            }

            existingLowStock?.Resolve();
        }
        else if (isLowStock && lowStockEnabled)
        {
            var message = $"{domainEvent.ProductName} tiene stock bajo ({domainEvent.NewQuantity} unidades, mínimo {domainEvent.MinimumStock}).";
            if (existingLowStock != null)
            {
                existingLowStock.RefreshStockInfo(AlertSeverity.Medium, message, domainEvent.NewQuantity);
            }
            else
            {
                newAlert = new Alert(domainEvent.BusinessId, domainEvent.ProductId, null, domainEvent.ProductName,
                    AlertType.LowStock, AlertSeverity.Medium, message, domainEvent.NewQuantity, domainEvent.MinimumStock,
                    null, domainEvent.WarehouseId);
                await alertRepository.AddAsync(newAlert, cancellationToken);
            }

            existingOutOfStock?.Resolve();
        }
        else
        {
            // Stock is healthy again — clear any previously active alert for this product+warehouse.
            existingLowStock?.Resolve();
            existingOutOfStock?.Resolve();
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        // Only notify on a brand-new alert, not every refresh of an
        // already-active one — otherwise every sale/intake touching a
        // product that's still low would re-notify on the same situation.
        if (newAlert != null) await notificationDispatcher.NotifyAsync(newAlert, cancellationToken);
    }
}
