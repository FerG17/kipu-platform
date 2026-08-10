using Bodega.Platform.Alerts.Application.Internal.OutboundServices;
using Bodega.Platform.Alerts.Domain.Model.Aggregates;

namespace Bodega.Platform.Alerts.Infrastructure.Notifications;

/// <summary>
///     Default implementation of IAlertNotificationDispatcher — does nothing,
///     on purpose. Wired up in Program.cs as the placeholder until a real
///     email/push implementation exists; deliberately does NOT call
///     alert.MarkNotified(), since no notification actually went out.
/// </summary>
public class NoOpAlertNotificationDispatcher : IAlertNotificationDispatcher
{
    public Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
