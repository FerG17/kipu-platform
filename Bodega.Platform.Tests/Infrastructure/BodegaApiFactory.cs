using Microsoft.AspNetCore.Mvc.Testing;

namespace Bodega.Platform.Tests.Infrastructure;

/// <summary>
///     Boots the real application against a dedicated MySQL database
///     (`bodega_platform_test`) on the same Docker container used for
///     development. Deliberately NOT SQLite/in-memory: several of the bugs
///     these tests cover live precisely in MySQL-specific behaviour —
///     foreign key RESTRICT on delete, decimal(10,2) truncation — which an
///     in-memory provider would silently not reproduce.
///
///     Configuration is injected through environment variables rather than
///     ConfigureAppConfiguration because Program.cs reads both the
///     connection string and the JWT secret while the builder is still being
///     assembled, before any WebApplicationFactory hook could override them.
/// </summary>
public class BodegaApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Any value ≥ 32 bytes gets past JwtSecretGuard; generated for the suite, it never leaves the test process.</summary>
    private const string TestJwtSecret = "integration-test-signing-key-not-used-anywhere-else-0123456789";

    static BodegaApiFactory()
    {
        // The database password used to be a literal in this file, in a public
        // repository. It comes from the same untracked .env Docker Compose
        // reads, so the container and the suite cannot drift apart —
        // see LocalEnvironment.
        var databasePassword = LocalEnvironment.Require("BODEGA_DB_PASSWORD");
        var testConnectionString =
            $"server=localhost;port=3307;user=root;password={databasePassword};database=bodega_platform_test";

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", testConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // The alerts sweep runs once on startup and then on this interval —
        // pushed far out so it can't fire mid-test and mutate alert state
        // underneath an assertion.
        Environment.SetEnvironmentVariable("Alerts__SweepIntervalHours", "24");

        // Every test signs up its own business, and they all look like one
        // client to the per-IP rate limiter. At the production budget of 10
        // sign-ups a minute the suite throttles itself with 429s as soon as
        // it grows past a handful of tests — which is the limiter working
        // correctly, not a bug, so the tests raise the ceiling instead.
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitsPerMinute", "10000");
        Environment.SetEnvironmentVariable("RateLimiting__GlobalPermitsPerMinute", "10000");
    }
}
