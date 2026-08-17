using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Tests.Infrastructure;

/// <summary>
///     Boots the real application against a dedicated MySQL database
///     (`kipu_platform_test`) on the same Docker container used for
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
public class KipuApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Any value ≥ 32 bytes gets past JwtSecretGuard; generated for the suite, it never leaves the test process.</summary>
    private const string TestJwtSecret = "integration-test-signing-key-not-used-anywhere-else-0123456789";

    /// <summary>Any value ≥ 20 bytes gets past BootstrapKeyGuard; see IntegrationTestBase.CreateBusinessWithOwnerAsync, which sends it as X-Bootstrap-Key.</summary>
    public const string TestBootstrapKey = "integration-test-bootstrap-key-not-used-anywhere-else";

    static KipuApiFactory()
    {
        // The database password used to be a literal in this file, in a public
        // repository. It comes from the same untracked .env Docker Compose
        // reads, so the container and the suite cannot drift apart —
        // see LocalEnvironment.
        var databasePassword = LocalEnvironment.Require("BODEGA_DB_PASSWORD");
        var testConnectionString =
            $"server=localhost;port=3307;user=root;password={databasePassword};database=kipu_platform_test";

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", testConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Bootstrap__Key", TestBootstrapKey);
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

    /// <summary>Swaps the real (SendGrid or logging) email service for one tests can read the sent code back from.</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<CapturingEmailService>();
            services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<CapturingEmailService>());
        });
    }
}
