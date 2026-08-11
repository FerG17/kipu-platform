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
    private const string TestConnectionString =
        "server=localhost;port=3307;user=root;password=dev_password;database=bodega_platform_test";

    /// <summary>Any value ≥ 32 bytes gets past JwtSecretGuard; it never leaves the test process.</summary>
    private const string TestJwtSecret = "integration-test-signing-key-not-used-anywhere-else-0123456789";

    static BodegaApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // The alerts sweep runs once on startup and then on this interval —
        // pushed far out so it can't fire mid-test and mutate alert state
        // underneath an assertion.
        Environment.SetEnvironmentVariable("Alerts__SweepIntervalHours", "24");
    }
}
