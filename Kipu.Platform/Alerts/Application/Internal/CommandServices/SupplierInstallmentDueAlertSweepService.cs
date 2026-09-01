using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Suppliers.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Alerts.Application.Internal.CommandServices;

/// <summary>Mirrors InstallmentDueAlertSweepService (X6 #7) exactly, for credit purchase orders instead of credit sales (X6 #12).</summary>
public class SupplierInstallmentDueAlertSweepService(
    IAlertRepository alertRepository,
    IAlertRuleRepository alertRuleRepository,
    IUnitOfWork unitOfWork,
    IAlertNotificationDispatcher notificationDispatcher,
    ILogger<SupplierInstallmentDueAlertSweepService> logger)
    : ISupplierInstallmentDueAlertSweepService
{
    public async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingSupplierInstallmentInfo> pendingInstallments,
        DateOnly today, CancellationToken cancellationToken)
    {
        var rule = await alertRuleRepository.FindByBusinessIdAndTypeAsync(businessId, AlertType.SupplierInstallmentDue, cancellationToken);
        var enabled = rule?.Enabled ?? true;
        var thresholdDays = rule?.ThresholdValue ?? InstallmentDueRules.DueSoonThresholdDays;

        var purchaseOrderIds = pendingInstallments.Select(info => info.PurchaseOrderId).ToList();
        var existingAlertsByPurchaseOrderId = (await alertRepository.FindActiveSupplierInstallmentAlertsByPurchaseOrderIdsAsync(
                purchaseOrderIds, cancellationToken))
            .GroupBy(alert => alert.PurchaseOrderId!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var newAlerts = new List<Alert>();

        foreach (var info in pendingInstallments)
        {
            existingAlertsByPurchaseOrderId.TryGetValue(info.PurchaseOrderId, out var existing);
            var daysRemaining = info.DueDate.DayNumber - today.DayNumber;
            var isDueSoon = enabled && InstallmentDueRules.IsDueSoon(info.DueDate, today, thresholdDays);

            if (!isDueSoon)
            {
                existing?.Resolve();
                continue;
            }

            var supplierLabel = info.SupplierName ?? "proveedor desconocido";
            var severity = daysRemaining < 0 ? AlertSeverity.High : AlertSeverity.Medium;
            var message = daysRemaining < 0
                ? $"Cuota de S/ {info.Amount:0.00} a {supplierLabel} venció hace {-daysRemaining} día(s)."
                : $"Cuota de S/ {info.Amount:0.00} a {supplierLabel} vence en {daysRemaining} día(s).";

            if (existing != null)
            {
                existing.RefreshInstallmentInfo(severity, message, daysRemaining);
            }
            else
            {
                var alert = Alert.ForSupplierInstallmentDue(businessId, info.PurchaseOrderId, info.SupplierName, severity, message,
                    info.Amount, daysRemaining);
                await alertRepository.AddAsync(alert, cancellationToken);
                newAlerts.Add(alert);
            }
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        if (newAlerts.Count > 0)
            logger.LogWarning("Supplier-installment-due alert sweep created {NewAlertCount} new alert(s) for business {BusinessId}",
                newAlerts.Count, businessId);

        foreach (var alert in newAlerts)
            await notificationDispatcher.NotifyAsync(alert, cancellationToken);
    }
}
