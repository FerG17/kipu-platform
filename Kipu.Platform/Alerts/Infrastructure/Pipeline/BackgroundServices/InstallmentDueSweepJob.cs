using Kipu.Platform.Shared.Application;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Sales.Interfaces.Acl;

namespace Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

/// <summary>
///     The scheduled half of the installment-due alerts engine (X6 #7) —
///     mirrors AlertExpirationSweepJob's role/reasoning exactly, but for
///     credit sales' cuota calendars instead of batch expirations. Runs
///     every Alerts:InstallmentDueSweepIntervalHours (default 6h, its own
///     independent interval — kept separate from the expiration sweep's so
///     one can be tuned without affecting the other).
/// </summary>
public class InstallmentDueSweepJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<InstallmentDueSweepJob> logger,
    IBusinessClock businessClock)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = configuration.GetValue("Alerts:InstallmentDueSweepIntervalHours", 6.0);
        var interval = TimeSpan.FromHours(intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Installment-due alert sweep failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    /// <summary>
    ///     Groups every pending installment by business and sweeps each
    ///     business in its own scope/DbContext/commit — same isolation
    ///     reasoning as AlertExpirationSweepJob.RunSweepAsync.
    /// </summary>
    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var discoveryScope = scopeFactory.CreateScope();
        var salesContextFacade = discoveryScope.ServiceProvider.GetRequiredService<ISalesContextFacade>();
        var pendingInstallments = await salesContextFacade.GetPendingInstallmentsForDueSweep(cancellationToken);
        var today = businessClock.Today;

        foreach (var businessInstallments in pendingInstallments.GroupBy(info => info.BusinessId))
        {
            try
            {
                await SweepBusinessAsync(businessInstallments.Key, businessInstallments.ToList(), today, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Installment-due alert sweep failed for business {BusinessId}", businessInstallments.Key);
            }
        }
    }

    private async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingInstallmentInfo> pendingInstallments,
        DateOnly today, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sweepService = scope.ServiceProvider.GetRequiredService<IInstallmentDueAlertSweepService>();
        await sweepService.SweepBusinessAsync(businessId, pendingInstallments, today, cancellationToken);
    }
}
