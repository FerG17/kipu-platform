namespace Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

/// <summary>
///     Fails startup when Alerts:SweepIntervalHours is zero or negative,
///     instead of letting AlertExpirationSweepJob find out the hard way at
///     runtime: Task.Delay(TimeSpan.Zero) doesn't actually wait (spins the
///     sweep in a tight loop), and a negative TimeSpan makes Task.Delay
///     throw — which happens outside RunSweepAsync's own try/catch and
///     permanently kills the BackgroundService, with no retry until the next
///     app restart. Mirrors JwtSecretGuard/BootstrapKeyGuard/
///     CorsAllowedOriginsGuard's reasoning: a misconfigured value must be a
///     loud crash at startup, never a silent downgrade.
/// </summary>
public static class AlertSweepIntervalGuard
{
    public static void EnsureUsable(double intervalHours)
    {
        if (intervalHours <= 0)
            throw new InvalidOperationException(
                $"Alerts:SweepIntervalHours must be greater than 0 (was {intervalHours}).");
    }
}
