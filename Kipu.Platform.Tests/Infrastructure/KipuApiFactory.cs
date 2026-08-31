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
        // Pool/timeout raised above MySqlConnector's defaults (Maximum Pool
        // Size=100, Connection Timeout=15s) as cheap insurance against
        // connection contention during a full-suite run (211 tests, each
        // opening its own scoped connection, several building their own
        // extra isolated WebApplicationFactory on top) — a mitigation, not a
        // proven fix, for occasional full-suite-only flakiness investigated
        // in PasswordResetTests.RequestingANewCode_InvalidatesThePreviousOne.
        var testConnectionString =
            $"server=localhost;port=3307;user=root;password={databasePassword};database=kipu_platform_test;" +
            "Maximum Pool Size=200;Connection Timeout=30";

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", testConnectionString);
        Environment.SetEnvironmentVariable("TokenSettings__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Bootstrap__Key", TestBootstrapKey);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // SessionCookieTests/CsrfProtectionTests send Origin: http://localhost:5173
        // to exercise the real trusted-origin check — previously only true
        // because every dev machine's untracked appsettings.Development.json
        // happens to list it too. Set explicitly so the suite doesn't
        // silently depend on a file that isn't checked in (found when this
        // ran in CI, which has no such file, for the first time).
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:5173");

        // The alerts sweeps run once on startup and then on this interval —
        // pushed far out so they can't fire mid-test and mutate alert state
        // underneath an assertion.
        Environment.SetEnvironmentVariable("Alerts__SweepIntervalHours", "24");
        Environment.SetEnvironmentVariable("Alerts__InstallmentDueSweepIntervalHours", "24");
        Environment.SetEnvironmentVariable("Alerts__SupplierInstallmentDueSweepIntervalHours", "24");

        // Every test signs up its own business, and they all look like one
        // client to the per-IP rate limiter. At the production budget of 10
        // sign-ups a minute the suite throttles itself with 429s as soon as
        // it grows past a handful of tests — which is the limiter working
        // correctly, not a bug, so the tests raise the ceiling instead.
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitsPerMinute", "10000");
        Environment.SetEnvironmentVariable("RateLimiting__GlobalPermitsPerMinute", "10000");

        // Production's real cooldown (appsettings.json) would make every test
        // that requests two codes back-to-back collide with it, since the
        // suite has no injectable clock to fast-forward past it yet. Zeroed
        // here; CooldownTests spins up its own factory with a real value to
        // verify the cooldown actually works.
        Environment.SetEnvironmentVariable("PasswordReset__RequestCooldownSeconds", "0");
    }

    /// <summary>Swaps the real (Resend or logging) email service for one tests can read the sent code back from.</summary>
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
