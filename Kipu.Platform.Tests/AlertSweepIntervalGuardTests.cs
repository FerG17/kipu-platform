using Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 M8 (bajo): the guard that decides whether this application is
///     allowed to boot with a zero or negative Alerts:SweepIntervalHours. A
///     pure unit test, no host and no database — mirrors
///     JwtSecretGuardTests/BootstrapKeyGuardTests/CorsAllowedOriginsGuardTests.
/// </summary>
public class AlertSweepIntervalGuardTests
{
    [Fact]
    public void ZeroInterval_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => AlertSweepIntervalGuard.EnsureUsable(0));
    }

    [Fact]
    public void NegativeInterval_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => AlertSweepIntervalGuard.EnsureUsable(-1));
    }

    [Fact]
    public void PositiveInterval_IsAccepted()
    {
        AlertSweepIntervalGuard.EnsureUsable(6.0);
    }
}
