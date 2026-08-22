using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.CommandServices;

public class ExpirationAlertSweepService(
    IAlertRepository alertRepository,
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IAlertNotificationDispatcher notificationDispatcher,
    ILogger<ExpirationAlertSweepService> logger)
    : IExpirationAlertSweepService
{
    public async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<ActiveBatchInfo> batches, DateOnly today,
        CancellationToken cancellationToken)
    {
        var rules = await LoadExpirationRules(businessId, cancellationToken);
        if (!rules.ExpiringSoonEnabled && !rules.ExpiredEnabled) return;

        // One batched lookup for the whole business instead of two queries
        // (existingExpired/existingExpiringSoon) per batch — was an N+1 that
        // scaled with every active batch across every business.
        var batchIds = batches.Select(batch => batch.BatchId).ToList();
        var existingAlertsByBatchAndType = (await alertRepository.FindActiveExpirationAlertsByBatchIdsAsync(batchIds, cancellationToken))
            .GroupBy(alert => (alert.BatchId!.Value, alert.Type))
            .ToDictionary(group => group.Key, group => group.First());

        var newAlerts = new List<Alert>();

        foreach (var batch in batches)
        {
            var thresholdDays = rules.ThresholdDays;
            var isExpired = ExpirationRules.IsExpired(batch.Expiration, today) && rules.ExpiredEnabled;
            var isExpiringSoon = ExpirationRules.IsExpiringSoon(batch.Expiration, today, thresholdDays)
                                 && rules.ExpiringSoonEnabled;
            var daysToExpiry = batch.Expiration?.DayNumber - today.DayNumber;

            existingAlertsByBatchAndType.TryGetValue((batch.BatchId, AlertType.Expired), out var existingExpired);
            existingAlertsByBatchAndType.TryGetValue((batch.BatchId, AlertType.Expiration), out var existingExpiringSoon);

            if (isExpired)
            {
                var message = $"{batch.ProductName} venció.";
                if (existingExpired != null)
                {
                    existingExpired.RefreshStockInfo(AlertSeverity.High, message, 0);
                }
                else
                {
                    var alert = new Alert(batch.BusinessId, batch.ProductId, batch.BatchId, batch.ProductName,
                        AlertType.Expired, AlertSeverity.High, message, 0, 0, daysToExpiry);
                    await alertRepository.AddAsync(alert, cancellationToken);
                    newAlerts.Add(alert);
                }

                existingExpiringSoon?.Resolve();
            }
            else if (isExpiringSoon)
            {
                var message = $"{batch.ProductName} vence en {daysToExpiry} día(s).";
                if (existingExpiringSoon != null)
                {
                    existingExpiringSoon.RefreshStockInfo(AlertSeverity.Medium, message, 0);
                }
                else
                {
                    var alert = new Alert(batch.BusinessId, batch.ProductId, batch.BatchId, batch.ProductName,
                        AlertType.Expiration, AlertSeverity.Medium, message, 0, 0, daysToExpiry);
                    await alertRepository.AddAsync(alert, cancellationToken);
                    newAlerts.Add(alert);
                }

                existingExpired?.Resolve();
            }
            else
            {
                existingExpired?.Resolve();
                existingExpiringSoon?.Resolve();
            }
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        if (newAlerts.Count > 0)
            logger.LogWarning("Expiration alert sweep created {NewAlertCount} new alert(s) across {BatchCount} active batch(es) for business {BusinessId}",
                newAlerts.Count, batches.Count, businessId);

        foreach (var alert in newAlerts)
            await notificationDispatcher.NotifyAsync(alert, cancellationToken);
    }

    /// <summary>
    ///     EXPIRATION and EXPIRED are separate, independently switchable
    ///     rules; only the first used to be read, so turning EXPIRED off did
    ///     nothing and turning EXPIRATION off silently killed both.
    /// </summary>
    private async Task<ExpirationRuleSettings> LoadExpirationRules(int businessId, CancellationToken cancellationToken)
    {
        var expiringSoonRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.Expiration,
            cancellationToken);
        var expiredRule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.Expired,
            cancellationToken);

        return new ExpirationRuleSettings(
            expiringSoonRule?.Enabled ?? true,
            expiredRule?.Enabled ?? true,
            expiringSoonRule?.ThresholdValue ?? ExpirationRules.ExpiringSoonThresholdDays);
    }

    private readonly record struct ExpirationRuleSettings(bool ExpiringSoonEnabled, bool ExpiredEnabled, int ThresholdDays);
}
