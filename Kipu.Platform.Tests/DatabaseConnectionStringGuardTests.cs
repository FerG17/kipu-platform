using Kipu.Platform.Shared.Infrastructure.Security;

namespace Kipu.Platform.Tests;

/// <summary>
///     The guard that decides whether the database connection string is
///     usable before it ever reaches the MySQL driver. A pure unit test, no
///     host and no database — mirrors JwtSecretGuardTests/BootstrapKeyGuardTests.
/// </summary>
public class DatabaseConnectionStringGuardTests
{
    [Fact]
    public void AMissingConnectionString_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConnectionStringGuard.EnsureUsable(null));
        Assert.Throws<InvalidOperationException>(() => DatabaseConnectionStringGuard.EnsureUsable("   "));
    }

    /// <summary>The env var never reached the process, so the literal template survived.</summary>
    [Fact]
    public void AnUnexpandedPlaceholder_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringGuard.EnsureUsable("%BODEGA_DB_CONNECTION_STRING%"));
    }

    /// <summary>Railway's own MYSQL_URL format (mysql://user:pass@host:port/db) — this driver expects key=value pairs, not a URI.</summary>
    [Fact]
    public void AUrlStyleConnectionString_RefusesToStart()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DatabaseConnectionStringGuard.EnsureUsable("mysql://root:secret@mysql.railway.internal:3306/railway"));
    }

    [Fact]
    public void AProperConnectionString_IsAccepted()
    {
        DatabaseConnectionStringGuard.EnsureUsable(
            "server=mysql.railway.internal;port=3306;user=root;password=secret;database=railway");
    }

    /// <summary>The message must never carry the password — startup errors land in logs and consoles.</summary>
    [Fact]
    public void TheFailureMessage_NeverContainsTheConnectionString()
    {
        const string connectionString = "%BODEGA_DB_CONNECTION_STRING%";

        var exception = Assert.Throws<InvalidOperationException>(
            () => DatabaseConnectionStringGuard.EnsureUsable(connectionString));

        Assert.DoesNotContain(connectionString, exception.Message, StringComparison.Ordinal);
    }
}
