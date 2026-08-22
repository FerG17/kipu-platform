using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Products.Domain.Model.Events;
using Kipu.Platform.Shared.Application.Internal.EventHandlers;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.EventHandlers;

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
