using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Sales.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.CommandServices;

public class InstallmentDueAlertSweepService(
    IAlertRepository alertRepository,
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IAlertNotificationDispatcher notificationDispatcher,
    ILogger<InstallmentDueAlertSweepService> logger)
    : IInstallmentDueAlertSweepService
{
    public async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingInstallmentInfo> pendingInstallments,
        DateOnly today, CancellationToken cancellationToken)
    {
        var rule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.InstallmentDue, cancellationToken);
        var enabled = rule?.Enabled ?? true;
        var thresholdDays = rule?.ThresholdValue ?? InstallmentDueRules.DueSoonThresholdDays;

        var saleIds = pendingInstallments.Select(info => info.SaleId).ToList();
        var existingAlertsBySaleId = (await alertRepository.FindActiveInstallmentAlertsBySaleIdsAsync(saleIds, cancellationToken))
            .GroupBy(alert => alert.SaleId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var newAlerts = new List<Alert>();

        foreach (var info in pendingInstallments)
        {
            existingAlertsBySaleId.TryGetValue(info.SaleId, out var existing);
            var daysRemaining = info.DueDate.DayNumber - today.DayNumber;
            var isDueSoon = enabled && InstallmentDueRules.IsDueSoon(info.DueDate, today, thresholdDays);

            if (!isDueSoon)
            {
                existing?.Resolve();
                continue;
            }

            var customerLabel = info.CustomerName ?? "cliente anónimo";
            var severity = daysRemaining < 0 ? AlertSeverity.High : AlertSeverity.Medium;
            var message = daysRemaining < 0
                ? $"Cuota de S/ {info.Amount:0.00} de {customerLabel} venció hace {-daysRemaining} día(s)."
                : $"Cuota de S/ {info.Amount:0.00} de {customerLabel} vence en {daysRemaining} día(s).";

            if (existing != null)
            {
                existing.RefreshInstallmentInfo(severity, message, daysRemaining);
            }
            else
            {
                var alert = Alert.ForInstallmentDue(businessId, info.SaleId, info.CustomerName, severity, message,
                    info.Amount, daysRemaining);
                await alertRepository.AddAsync(alert, cancellationToken);
                newAlerts.Add(alert);
            }
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        if (newAlerts.Count > 0)
            logger.LogWarning("Installment-due alert sweep created {NewAlertCount} new alert(s) for business {BusinessId}",
                newAlerts.Count, businessId);

        foreach (var alert in newAlerts)
            await notificationDispatcher.NotifyAsync(alert, cancellationToken);
    }
}
