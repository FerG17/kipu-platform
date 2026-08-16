using Kipu.Platform.Shared.Domain.Model.Events;

namespace Kipu.Platform.Products.Domain.Model.Events;

/// <summary>
///     Raised when a batch is retired (goods thrown out or returned). Alerts
///     subscribes to close any expiration warning still open for it, so
///     discarding the goods is a single action for the shop rather than
///     "retire the batch, then also go resolve the alert it left behind".
/// </summary>
public record BatchDiscardedEvent(int BatchId, int ProductId, int BusinessId) : IEvent;
