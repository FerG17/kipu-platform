using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     The session cookie moved to SameSite=None in production (see
///     AuthenticationController.SetSessionCookie) — required so it still
///     reaches the API from a Cloudflare Pages frontend calling a Railway
///     backend, two different sites by the Public Suffix List. That trades
///     away the browser's own cross-site defense for this cookie, so
///     RequestAuthorizationMiddleware.HasTrustedOrigin picks it back up by
///     requiring a trusted Origin/Referer on every cookie-authenticated
///     state-changing request. The test host is Development, where the
///     cookie is still SameSite=Strict, but the Origin/Referer check itself
///     runs regardless of environment — these tests exercise it directly by
///     driving requests with the cookie the way a browser actually would.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class CsrfProtectionTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private async Task<HttpClient> SignUpWithCookieAsync()
    {
        var client = factory.CreateClient();
        var response = await PostSignUpAsync(client, new
        {
            email = $"csrf-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Csrf",
            lastName = "Test",
            businessName = "Kipu csrf test",
            businessType = "RETAIL"
        });
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>
    ///     No Origin, no Referer — exactly what a forged cross-site POST from
    ///     another page looks like once the ambient cookie is attached by the
    ///     browser. Must not be allowed to reach the handler.
    /// </summary>
    [Fact]
    public async Task CookieAuthenticatedPost_WithNoOriginOrReferer_IsRejected()
    {
        var client = await SignUpWithCookieAsync();

        var response = await client.PostAsync("/api/v1/authentication/sign-out", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>An Origin naming a site this deployment never listed in Cors:AllowedOrigins is just as untrusted as none at all.</summary>
    [Fact]
    public async Task CookieAuthenticatedPost_WithUntrustedOrigin_IsRejected()
    {
        var client = await SignUpWithCookieAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authentication/sign-out");
        request.Headers.Add("Origin", "https://attacker.example");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>The real frontend's own origin, exactly as configured in Cors:AllowedOrigins, must keep working.</summary>
    [Fact]
    public async Task CookieAuthenticatedPost_WithTrustedOrigin_Succeeds()
    {
        var client = await SignUpWithCookieAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authentication/sign-out");
        request.Headers.Add("Origin", "http://localhost:5173");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    ///     A safe (read-only) method carries no CSRF risk on its own — a
    ///     cross-site page can trigger one just by loading an image tag, so
    ///     requiring an Origin here would only break real usage without
    ///     stopping anything an attacker couldn't already do.
    /// </summary>
    [Fact]
    public async Task CookieAuthenticatedGet_WithNoOrigin_StillSucceeds()
    {
        var client = await SignUpWithCookieAsync();

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    ///     The Bearer/header path (Swagger, this very test suite, any future
    ///     API consumer) has no ambient-credential problem — a malicious page
    ///     cannot make the victim's browser attach a header it doesn't know,
    ///     unlike a cookie. The Origin check must not apply to it.
    /// </summary>
    [Fact]
    public async Task HeaderAuthenticatedPost_WithNoOrigin_StillSucceeds()
    {
        var response = await PostSignUpAsync(Client, new
        {
            email = $"csrf-header-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Csrf",
            lastName = "Header",
            businessName = "Kipu csrf header test",
            businessType = "RETAIL"
        });
        response.EnsureSuccessStatusCode();
        var token = (await ReadJsonAsync(response)).GetProperty("token").GetString()!;

        var headerClient = AuthenticatedClient(token);
        var signOutResponse = await headerClient.PostAsync("/api/v1/authentication/sign-out", null);

        Assert.Equal(HttpStatusCode.NoContent, signOutResponse.StatusCode);
    }
}
