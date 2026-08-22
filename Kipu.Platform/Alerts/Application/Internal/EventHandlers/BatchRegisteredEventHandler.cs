using Microsoft.Extensions.Logging;
using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Products.Domain.Model.Events;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Application.Internal.EventHandlers;
using Kipu.Platform.Shared.Domain.Model.Services;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.EventHandlers;

/// <summary>
///     Reactive half of the alerts engine (§5.4) for expiration: re-evaluates
///     a batch's expiration the moment it's created/updated. The
///     time-only-passed case (nothing "happens", a batch just crosses the
///     threshold) is covered separately by AlertExpirationSweepJob.
/// </summary>
public class BatchRegisteredEventHandler(
    IAlertRepository alertRepository,
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IAlertNotificationDispatcher notificationDispatcher,
    ILogger<BatchRegisteredEventHandler> logger,
    IBusinessClock businessClock)
    : IEventHandler<BatchRegisteredEvent>
{
    public async Task Handle(BatchRegisteredEvent domainEvent, CancellationToken cancellationToken)
    {
        // Two independent rules: EXPIRATION governs the "about to expire"
        // warning, EXPIRED the "already expired" one. Only the first used to
        // be read, so turning EXPIRED off did nothing at all, and turning
        // EXPIRATION off silently killed both.
        var expiringSoonRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(domainEvent.BusinessId,
            AlertType.Expiration, cancellationToken);
        var expiredRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(domainEvent.BusinessId,
            AlertType.Expired, cancellationToken);
        var expiringSoonEnabled = expiringSoonRule?.Enabled ?? true;
        var expiredEnabled = expiredRule?.Enabled ?? true;
        if (!expiringSoonEnabled && !expiredEnabled) return;

        var thresholdDays = expiringSoonRule?.ThresholdValue ?? ExpirationRules.ExpiringSoonThresholdDays;
        var today = businessClock.Today;

        var existingExpired = await alertRepository.FindActiveByProductAndTypeAsync(domainEvent.ProductId, AlertType.Expired,
            domainEvent.BatchId, null, cancellationToken);
        var existingExpiringSoon = await alertRepository.FindActiveByProductAndTypeAsync(domainEvent.ProductId,
            AlertType.Expiration, domainEvent.BatchId, null, cancellationToken);

        var isExpired = ExpirationRules.IsExpired(domainEvent.Expiration, today) && expiredEnabled;
        var isExpiringSoon = ExpirationRules.IsExpiringSoon(domainEvent.Expiration, today, thresholdDays) && expiringSoonEnabled;
        var daysToExpiry = domainEvent.Expiration?.DayNumber - today.DayNumber;

        Alert? newAlert = null;

        if (isExpired)
        {
            var message = $"{domainEvent.ProductName} venció.";
            if (existingExpired != null)
            {
                existingExpired.RefreshStockInfo(AlertSeverity.High, message, 0);
            }
            else
            {
                newAlert = new Alert(domainEvent.BusinessId, domainEvent.ProductId, domainEvent.BatchId,
                    domainEvent.ProductName, AlertType.Expired, AlertSeverity.High, message, 0, 0, daysToExpiry);
                await alertRepository.AddAsync(newAlert, cancellationToken);
            }

            existingExpiringSoon?.Resolve();
        }
        else if (isExpiringSoon)
        {
            var message = $"{domainEvent.ProductName} vence en {daysToExpiry} día(s).";
            if (existingExpiringSoon != null)
            {
                existingExpiringSoon.RefreshStockInfo(AlertSeverity.Medium, message, 0);
            }
            else
            {
                newAlert = new Alert(domainEvent.BusinessId, domainEvent.ProductId, domainEvent.BatchId,
                    domainEvent.ProductName, AlertType.Expiration, AlertSeverity.Medium, message, 0, 0, daysToExpiry);
                await alertRepository.AddAsync(newAlert, cancellationToken);
            }

            existingExpired?.Resolve();
        }
        else
        {
            existingExpired?.Resolve();
            existingExpiringSoon?.Resolve();
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        if (newAlert != null)
        {
            logger.LogWarning(
                "Alert {AlertType} ({Severity}) created for product {ProductId} ({ProductName}), batch {BatchId}, in business {BusinessId}: {Message}",
                newAlert.Type, newAlert.Severity, newAlert.ProductId, domainEvent.ProductName, domainEvent.BatchId,
                newAlert.BusinessId, newAlert.Message);
            await notificationDispatcher.NotifyAsync(newAlert, cancellationToken);
        }
    }
}
