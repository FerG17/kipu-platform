using Kipu.Platform.Shared.Domain.Model.Events;

namespace Kipu.Platform.Sales.Domain.Model.Events;

/// <summary>
///     Raised after a sale is confirmed. Stock has already been decremented
///     synchronously (via IProductContextFacade, in the same transaction —
///     required so an insufficient-stock line can reject the whole sale
///     atomically); this event is for other, non-transactional consumers
///     (Alerts, Dashboard) that only need to react afterward.
/// </summary>
public record SaleRegisteredEvent(int SaleId, int BusinessId, IReadOnlyCollection<(int ProductId, int Quantity)> Lines) : IEvent;
