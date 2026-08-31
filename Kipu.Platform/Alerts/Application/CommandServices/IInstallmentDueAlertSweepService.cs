using Kipu.Platform.Sales.Interfaces.Acl;

namespace Kipu.Platform.Alerts.Application.CommandServices;

/// <summary>
///     The per-business core of the installment-due alert sweep (X6 #7) —
///     mirrors IExpirationAlertSweepService's role: resolves alerts that no
///     longer qualify and creates/refreshes the ones that do. Shared between
///     InstallmentDueSweepJob (runs it for every business on a timer) and
///     AlertRuleCommandService (runs it once, immediately, for the one
///     business whose threshold or enabled flag just changed).
/// </summary>
public interface IInstallmentDueAlertSweepService
{
    Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingInstallmentInfo> pendingInstallments, DateOnly today,
        CancellationToken cancellationToken);
}
