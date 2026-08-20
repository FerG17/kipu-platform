using Kipu.Platform.Shared.Infrastructure.Security;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 S1 (Crítico): the guard that decides whether this application is
///     allowed to boot in Production with no allowed CORS origins configured.
///     A pure unit test, no host and no database — mirrors
///     JwtSecretGuardTests/BootstrapKeyGuardTests.
/// </summary>
public class CorsAllowedOriginsGuardTests
{
    [Fact]
    public void EmptyOriginsInProduction_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => CorsAllowedOriginsGuard.EnsureUsable([], isProduction: true));
    }

    [Fact]
    public void EmptyOriginsOutsideProduction_IsAccepted()
    {
        CorsAllowedOriginsGuard.EnsureUsable([], isProduction: false);
    }

    [Fact]
    public void NonEmptyOriginsInProduction_IsAccepted()
    {
        CorsAllowedOriginsGuard.EnsureUsable(["https://kipu.pages.dev"], isProduction: true);
    }
}
