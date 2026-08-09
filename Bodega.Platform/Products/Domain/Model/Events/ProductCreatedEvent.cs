using Bodega.Platform.Shared.Domain.Model.Events;

namespace Bodega.Platform.Products.Domain.Model.Events;

public record ProductCreatedEvent(int ProductId, int BusinessId, string Name) : IEvent;
