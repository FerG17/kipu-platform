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
}
