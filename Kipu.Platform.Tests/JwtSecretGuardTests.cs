using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

namespace Kipu.Platform.Tests;

/// <summary>
///     The guard that decides whether this application is allowed to boot at
///     all. A pure unit test, no host and no database: every one of these
///     cases is a deployment that must crash loudly instead of coming up and
///     signing tokens anyone could forge.
/// </summary>
public class JwtSecretGuardTests
{
    [Fact]
    public void AMissingSecret_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => JwtSecretGuard.EnsureUsable(null));
        Assert.Throws<InvalidOperationException>(() => JwtSecretGuard.EnsureUsable("   "));
    }

    /// <summary>The original incident: the placeholder itself became the HMAC key, and it is public in the repository.</summary>
    [Fact]
    public void AnUnexpandedPlaceholder_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => JwtSecretGuard.EnsureUsable("%BODEGA_JWT_SECRET%"));
    }

    /// <summary>Microsoft's provider only enforces 128 bits, which is how a 19-character placeholder once slipped through.</summary>
    [Fact]
    public void ASecretShorterThanTheMinimum_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => JwtSecretGuard.EnsureUsable(new string('k', 31)));
    }

    /// <summary>
    ///     The key that was committed to this repository while it was public.
    ///     It passes every other check — right length, no placeholder — which
    ///     is exactly why it needs naming: it is readable by anyone, forever,
    ///     because removing a file does not remove it from git history.
    /// </summary>
    [Fact]
    public void TheLeakedDevelopmentSecret_RefusesToStartInAnyEnvironment()
    {
        Assert.Throws<InvalidOperationException>(() =>
            JwtSecretGuard.EnsureUsable("bodega-local-dev-secret-do-not-use-in-production-1234567890"));
    }

    [Fact]
    public void AProperSecret_IsAccepted()
    {
        JwtSecretGuard.EnsureUsable(new string('k', 32));
    }

    /// <summary>The message must never carry the secret — startup errors land in logs and consoles.</summary>
    [Fact]
    public void TheFailureMessage_NeverContainsTheSecret()
    {
        const string secret = "%BODEGA_JWT_SECRET%";

        var exception = Assert.Throws<InvalidOperationException>(() => JwtSecretGuard.EnsureUsable(secret));

        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
    }
}
