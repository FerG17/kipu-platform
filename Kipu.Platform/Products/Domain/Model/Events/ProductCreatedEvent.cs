using Kipu.Platform.Shared.Domain.Model.Events;

namespace Kipu.Platform.Products.Domain.Model.Events;

public record ProductCreatedEvent(int ProductId, int BusinessId, string Name) : IEvent;
