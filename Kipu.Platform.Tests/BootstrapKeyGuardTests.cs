using Kipu.Platform.Iam.Infrastructure.Bootstrap;

namespace Kipu.Platform.Tests;

/// <summary>
///     The guard that decides whether this application is allowed to boot
///     with sign-up reachable at all. Pure unit test, no host and no
///     database — mirrors JwtSecretGuardTests, since a misconfigured
///     bootstrap key must fail startup the same way a misconfigured signing
///     key does, not silently leave sign-up open to anyone.
/// </summary>
public class BootstrapKeyGuardTests
{
    [Fact]
    public void AMissingKey_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => BootstrapKeyGuard.EnsureUsable(null));
        Assert.Throws<InvalidOperationException>(() => BootstrapKeyGuard.EnsureUsable("   "));
    }

    [Fact]
    public void AnUnexpandedPlaceholder_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => BootstrapKeyGuard.EnsureUsable("%BODEGA_BOOTSTRAP_KEY%"));
    }

    [Fact]
    public void AKeyShorterThanTheMinimum_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => BootstrapKeyGuard.EnsureUsable(new string('k', 19)));
    }

    [Fact]
    public void AProperKey_IsAccepted()
    {
        BootstrapKeyGuard.EnsureUsable(new string('k', 20));
    }

    /// <summary>The message must never carry the key — startup errors land in logs and consoles.</summary>
    [Fact]
    public void TheFailureMessage_NeverContainsTheKey()
    {
        const string key = "%BODEGA_BOOTSTRAP_KEY%";

        var exception = Assert.Throws<InvalidOperationException>(() => BootstrapKeyGuard.EnsureUsable(key));

        Assert.DoesNotContain(key, exception.Message, StringComparison.Ordinal);
    }
}
