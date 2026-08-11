using Bodega.Platform.Shared.Domain.Model.Events;

namespace Bodega.Platform.Products.Domain.Model.Events;

/// <summary>
///     Raised when a product is deactivated (the soft delete behind
///     DELETE /products/{id}). Alerts subscribes to close anything still
///     open for it — otherwise a low-stock or expiration alert outlives the
///     product it refers to and sits in the list pointing at something the
///     shop already removed from its catalog.
/// </summary>
public record ProductDeactivatedEvent(int ProductId, int BusinessId) : IEvent;
