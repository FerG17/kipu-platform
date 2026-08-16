using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Products.Domain.Model.Events;
using Kipu.Platform.Shared.Application.Internal.EventHandlers;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>
///     Closes everything still open for a product once it is deactivated.
///     Otherwise a low-stock or expiration alert outlives the product it
///     refers to and sits in the list pointing at something the shop already
///     took out of its catalog.
/// </summary>
public class ProductDeactivatedEventHandler(IAlertRepository alertRepository, IUnitOfWork unitOfWork)
    : IEventHandler<ProductDeactivatedEvent>
{
    public async Task Handle(ProductDeactivatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var alerts = await alertRepository.FindActiveByProductIdAsync(domainEvent.ProductId, cancellationToken);

        foreach (var alert in alerts)
        {
            alert.Resolve();
            alertRepository.Update(alert);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
