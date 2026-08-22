using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;

namespace Kipu.Platform.Alerts.Infrastructure.Notifications;

/// <summary>
///     Default implementation of IAlertNotificationDispatcher — does nothing,
///     on purpose. Wired up in Program.cs as the placeholder until a real
///     email/push implementation exists. Alert.Notified/NotifiedAt stay at
///     their default (false/null) as long as this is the active dispatcher —
///     a real implementation will need its own way to record that a
///     notification actually went out.
/// </summary>
public class NoOpAlertNotificationDispatcher : IAlertNotificationDispatcher
{
    public Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
