using Kipu.Platform.Shared.Application;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Suppliers.Interfaces.Acl;

namespace Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

/// <summary>
///     The scheduled half of the supplier-installment-due alerts engine (X6
///     #12) — mirrors InstallmentDueSweepJob (X6 #7) exactly, for credit
///     purchase orders' cuota calendars instead of credit sales'. Runs every
///     Alerts:SupplierInstallmentDueSweepIntervalHours (default 6h, its own
///     independent interval).
/// </summary>
public class SupplierInstallmentDueSweepJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SupplierInstallmentDueSweepJob> logger,
    IBusinessClock businessClock)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = configuration.GetValue("Alerts:SupplierInstallmentDueSweepIntervalHours", 6.0);
        var interval = TimeSpan.FromHours(intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Supplier-installment-due alert sweep failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var discoveryScope = scopeFactory.CreateScope();
        var supplierContextFacade = discoveryScope.ServiceProvider.GetRequiredService<ISupplierContextFacade>();
        var pendingInstallments = await supplierContextFacade.GetPendingSupplierInstallmentsForDueSweep(cancellationToken);
        var today = businessClock.Today;

        foreach (var businessInstallments in pendingInstallments.GroupBy(info => info.BusinessId))
        {
            try
            {
                await SweepBusinessAsync(businessInstallments.Key, businessInstallments.ToList(), today, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Supplier-installment-due alert sweep failed for business {BusinessId}", businessInstallments.Key);
            }
        }
    }

    private async Task SweepBusinessAsync(int businessId, IReadOnlyCollection<PendingSupplierInstallmentInfo> pendingInstallments,
        DateOnly today, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sweepService = scope.ServiceProvider.GetRequiredService<ISupplierInstallmentDueAlertSweepService>();
        await sweepService.SweepBusinessAsync(businessId, pendingInstallments, today, cancellationToken);
    }
}
