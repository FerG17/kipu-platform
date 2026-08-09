using Cortex.Mediator.Notifications;
using Bodega.Platform.Shared.Domain.Model.Events;

namespace Bodega.Platform.Shared.Application.Internal.EventHandlers;

/// <summary>
///     Handles a domain event of type <typeparamref name="TEvent"/>.
///     Bounded contexts implement one handler per event they react to, keeping
///     cross-context reactions decoupled from the context that raised the
///     event. Extends Cortex.Mediator's INotificationHandler so any
///     implementation is auto-discovered and invoked by IMediator.PublishAsync.
/// </summary>
/// <typeparam name="TEvent">The domain event type this handler reacts to.</typeparam>
public interface IEventHandler<TEvent> : INotificationHandler<TEvent> where TEvent : IEvent
{
}
