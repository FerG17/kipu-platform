using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X3 "Endurecer tests": the real rate limiter (Program.cs's "auth"
///     policy, applied to every AuthenticationController endpoint) is raised
///     to 10000/min for the whole suite (KipuApiFactory), so nothing ever
///     proved the real, much stricter production limit actually rejects with
///     429 once exceeded. This builds one dedicated, throwaway host with the
///     limit lowered back down, just for this test.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class RateLimitingTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private const string AuthPermitsEnvVar = "RateLimiting__AuthPermitsPerMinute";

    /// <summary>
    ///     Program.cs reads RateLimiting:AuthPermitsPerMinute into a plain int
    ///     local, baked into the RateLimitPartition factory closure at
    ///     AddRateLimiter registration time — not IOptions&lt;T&gt;-bound, so
    ///     neither a services.Configure&lt;T&gt;() override (works for
    ///     PasswordResetSettings elsewhere in this suite) nor
    ///     ConfigureAppConfiguration on a derived WebApplicationFactory
    ///     (verified empirically — does not take effect, confirming
    ///     KipuApiFactory's own doc comment on why it uses env vars instead)
    ///     reaches it in time. The only lever that works is the same one
    ///     KipuApiFactory itself uses: an environment variable, read while
    ///     Program.cs's builder is still being assembled — flipped here just
    ///     for the lifetime of one dedicated factory, then restored so it
    ///     can't leak into any other test sharing this collection.
    /// </summary>
    [Fact]
    public async Task ExceedingTheAuthRateLimit_Returns429()
    {
        const string limitedValue = "3";
        var originalValue = Environment.GetEnvironmentVariable(AuthPermitsEnvVar);
        Environment.SetEnvironmentVariable(AuthPermitsEnvVar, limitedValue);
        try
        {
            await using var limitedFactory = new WebApplicationFactory<Program>();
            var client = limitedFactory.CreateClient();

            HttpResponseMessage? lastResponse = null;
            for (var i = 0; i < int.Parse(limitedValue) + 1; i++)
            {
                lastResponse = await client.PostAsJsonAsync("/api/v1/authentication/sign-in",
                    new { email = "nobody@test.local", password = "wrong-password" });
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AuthPermitsEnvVar, originalValue);
        }
    }

    private const string TrustLastProxyHopEnvVar = "ForwardedHeaders__TrustLastProxyHop";

    /// <summary>
    ///     X4 S2: with TrustLastProxyHop on, two callers behind the same test
    ///     connection (TestServer gives every request the same
    ///     RemoteIpAddress, standing in for every caller sharing Railway's
    ///     edge address) must still get independent budgets once they present
    ///     different X-Forwarded-For values — proving the partition key
    ///     really switches off RemoteIpAddress once this is enabled.
    /// </summary>
    [Fact]
    public async Task WithTrustLastProxyHopEnabled_DifferentForwardedForValues_GetIndependentBudgets()
    {
        const string limitedValue = "3";
        var originalPermits = Environment.GetEnvironmentVariable(AuthPermitsEnvVar);
        var originalTrust = Environment.GetEnvironmentVariable(TrustLastProxyHopEnvVar);
        Environment.SetEnvironmentVariable(AuthPermitsEnvVar, limitedValue);
        Environment.SetEnvironmentVariable(TrustLastProxyHopEnvVar, "true");
        try
        {
            await using var limitedFactory = new WebApplicationFactory<Program>();
            var clientA = limitedFactory.CreateClient();
            clientA.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
            var clientB = limitedFactory.CreateClient();
            clientB.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.20");

            for (var i = 0; i < int.Parse(limitedValue); i++)
            {
                var responseA = await clientA.PostAsJsonAsync("/api/v1/authentication/sign-in",
                    new { email = "nobody@test.local", password = "wrong-password" });
                Assert.NotEqual(HttpStatusCode.TooManyRequests, responseA.StatusCode);

                var responseB = await clientB.PostAsJsonAsync("/api/v1/authentication/sign-in",
                    new { email = "nobody@test.local", password = "wrong-password" });
                Assert.NotEqual(HttpStatusCode.TooManyRequests, responseB.StatusCode);
            }

            // Each has now made exactly the limit — one more from either must 429,
            // which would not be true if they shared one collapsed bucket.
            var overLimitA = await clientA.PostAsJsonAsync("/api/v1/authentication/sign-in",
                new { email = "nobody@test.local", password = "wrong-password" });
            Assert.Equal(HttpStatusCode.TooManyRequests, overLimitA.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AuthPermitsEnvVar, originalPermits);
            Environment.SetEnvironmentVariable(TrustLastProxyHopEnvVar, originalTrust);
        }
    }

    /// <summary>
    ///     X4 S2, the flip side: with TrustLastProxyHop left at its default
    ///     (off), a caller cannot spoof X-Forwarded-For to dodge the limit —
    ///     two different header values from the same connection must still
    ///     share one bucket, keyed on RemoteIpAddress like before.
    /// </summary>
    [Fact]
    public async Task WithTrustLastProxyHopDisabled_ForwardedForIsIgnored_BudgetStaysShared()
    {
        const string limitedValue = "3";
        var originalPermits = Environment.GetEnvironmentVariable(AuthPermitsEnvVar);
        var originalTrust = Environment.GetEnvironmentVariable(TrustLastProxyHopEnvVar);
        Environment.SetEnvironmentVariable(AuthPermitsEnvVar, limitedValue);
        Environment.SetEnvironmentVariable(TrustLastProxyHopEnvVar, "false");
        try
        {
            await using var limitedFactory = new WebApplicationFactory<Program>();
            var clientA = limitedFactory.CreateClient();
            clientA.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
            var clientB = limitedFactory.CreateClient();
            clientB.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.20");

            HttpResponseMessage? lastResponse = null;
            for (var i = 0; i < int.Parse(limitedValue) + 1; i++)
            {
                // Alternate senders — if the header were honoured, neither alone
                // would ever hit its own limit within this loop.
                var client = i % 2 == 0 ? clientA : clientB;
                lastResponse = await client.PostAsJsonAsync("/api/v1/authentication/sign-in",
                    new { email = "nobody@test.local", password = "wrong-password" });
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AuthPermitsEnvVar, originalPermits);
            Environment.SetEnvironmentVariable(TrustLastProxyHopEnvVar, originalTrust);
        }
    }
}
