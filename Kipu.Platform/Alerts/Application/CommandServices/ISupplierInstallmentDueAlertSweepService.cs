using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Alerts.Application.CommandServices;

/// <summary>
///     The per-business core of the supplier-installment-due alert sweep (X6
///     #12) — mirrors IInstallmentDueAlertSweepService (X6 #7). Shared
///     between SupplierInstallmentDueSweepJob (runs it for every business on
///     a timer) and AlertRuleCommandService (runs it once, immediately, for
///     the one business whose threshold or enabled flag just changed).
/// </summary>
public interface ISupplierInstallmentDueAlertSweepService
{
    Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingSupplierInstallmentInfo> pendingInstallments, DateOnly today,
        CancellationToken cancellationToken);
}
