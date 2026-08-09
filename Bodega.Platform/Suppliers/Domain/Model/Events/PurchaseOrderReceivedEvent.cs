using Bodega.Platform.Shared.Domain.Model.Events;

namespace Bodega.Platform.Suppliers.Domain.Model.Events;

/// <summary>
///     Raised after a purchase order is marked RECEIVED. Stock has already
///     been registered synchronously (via IProductContextFacade.RegisterStockIntake,
///     in the same transaction — see PurchaseOrderCommandService); this event
///     is for other, non-transactional consumers (Alerts, Dashboard).
/// </summary>
public record PurchaseOrderReceivedEvent(
    int PurchaseOrderId,
    int BusinessId,
    string SupplierName,
    IReadOnlyCollection<(int ProductId, int Quantity)> Lines) : IEvent;
