using Kipu.Platform.Shared.Application;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;

namespace Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

/// <summary>
///     The "programado (sweep)" half of the alerts engine (§5.4): reactive
///     event handlers only fire when something actually happens to a batch
///     (created/updated); a product that crosses the "about to expire"
///     threshold purely because time passed — with nobody touching
///     inventory — needs a periodic scan instead. Runs every
///     Alerts:SweepIntervalHours (default 6h, configurable so tests/demos
///     can use a much shorter interval).
/// </summary>
public class AlertExpirationSweepJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AlertExpirationSweepJob> logger,
    IBusinessClock businessClock)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = configuration.GetValue("Alerts:SweepIntervalHours", 6.0);
        var interval = TimeSpan.FromHours(intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Alert expiration sweep failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    /// <summary>
    ///     Groups every active batch by business and sweeps each business in
    ///     its own scope/DbContext/commit — a persistence failure for one
    ///     business (e.g. a bad insert) is caught and logged there, and can
    ///     no longer roll back the alerts already computed and committed for
    ///     every other business in the same sweep cycle.
    /// </summary>
    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var discoveryScope = scopeFactory.CreateScope();
        var productContextFacade = discoveryScope.ServiceProvider.GetRequiredService<IProductContextFacade>();
        var batches = await productContextFacade.GetAllActiveBatchesForExpirationSweep(cancellationToken);
        var today = businessClock.Today;

        foreach (var businessBatches in batches.GroupBy(batch => batch.BusinessId))
        {
            try
            {
                await SweepBusinessAsync(businessBatches.Key, businessBatches.ToList(), today, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Alert expiration sweep failed for business {BusinessId}", businessBatches.Key);
            }
        }
    }

    private async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<ActiveBatchInfo> batches, DateOnly today,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sweepService = scope.ServiceProvider.GetRequiredService<IExpirationAlertSweepService>();
        await sweepService.SweepBusinessAsync(businessId, batches, today, cancellationToken);
    }
}
