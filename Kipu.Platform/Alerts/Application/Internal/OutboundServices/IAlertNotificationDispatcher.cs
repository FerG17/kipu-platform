using Kipu.Platform.Alerts.Domain.Model.Aggregates;

namespace Kipu.Platform.Alerts.Application.Internal.OutboundServices;

/// <summary>
///     Extension point for actually pushing a newly-created alert out to
///     someone (email, push notification, SMS, etc.) — deliberately not
///     implemented yet, this version only persists alerts and lets the user
///     pull them from the API/frontend. Alert already carries Notified/
///     NotifiedAt so a real implementation has somewhere to record that it
///     fired; swapping in a real dispatcher later is a single DI
///     registration change (see NoOpAlertNotificationDispatcher), no changes
///     needed to the event handlers or the sweep job that call this.
/// </summary>
public interface IAlertNotificationDispatcher
{
    Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default);
}
