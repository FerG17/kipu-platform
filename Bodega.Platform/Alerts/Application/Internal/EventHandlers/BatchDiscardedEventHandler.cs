using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Products.Domain.Model.Events;
using Bodega.Platform.Shared.Application.Internal.EventHandlers;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>
///     Closes any expiration alert still open for a batch once its goods
///     leave the shelf, so discarding them is one action for the shop rather
///     than "retire the batch, then also go and resolve the alert".
/// </summary>
public class BatchDiscardedEventHandler(IAlertRepository alertRepository, IUnitOfWork unitOfWork)
    : IEventHandler<BatchDiscardedEvent>
{
    public async Task Handle(BatchDiscardedEvent domainEvent, CancellationToken cancellationToken)
    {
        var alerts = await alertRepository.FindActiveByBatchIdAsync(domainEvent.BatchId, cancellationToken);

        foreach (var alert in alerts)
        {
            alert.Resolve();
            alertRepository.Update(alert);
        }

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
