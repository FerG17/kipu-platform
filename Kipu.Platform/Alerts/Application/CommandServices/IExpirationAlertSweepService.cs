using Kipu.Platform.Products.Interfaces.Acl;

namespace Kipu.Platform.Alerts.Application.CommandServices;

/// <summary>
///     The per-business core of the expiration alert sweep (§5.4): resolves
///     alerts that no longer meet the current EXPIRATION/EXPIRED rule
///     thresholds and creates the ones that now qualify. Shared between
///     AlertExpirationSweepJob (runs it for every business on a timer) and
///     AlertRuleCommandService (runs it once, immediately, for the one
///     business whose threshold or enabled flag just changed — see X5 #1).
/// </summary>
public interface IExpirationAlertSweepService
{
    Task SweepBusinessAsync(int businessId, IReadOnlyCollection<ActiveBatchInfo> batches, DateOnly today,
        CancellationToken cancellationToken);
}
