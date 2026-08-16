using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Covers the httpOnly session cookie added alongside the existing Bearer
///     header (see RequestAuthorizationMiddleware, which accepts either).
///     WebApplicationFactory's default client already tracks cookies per
///     HttpClient instance (HandleCookies defaults to true), so no manual
///     CookieContainer setup is needed here — just reusing the same client
///     across calls is enough to prove the cookie alone authenticates.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class SessionCookieTests : IntegrationTestBase
{
    private readonly BodegaApiFactory _factory;

    public SessionCookieTests(BodegaApiFactory factory) : base(factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     WebApplicationFactory's default client (HandleCookies=true) parses
    ///     Set-Cookie into its own CookieContainer and hands back a trimmed
    ///     "name=value" copy on Headers — the real HttpOnly/SameSite/Path
    ///     attributes never reach test code that way. Turning HandleCookies
    ///     off for this one client is what lets the test actually see the
    ///     raw header the browser would.
    /// </summary>
    [Fact]
    public async Task SignUp_SetsSessionCookie_WithHttpOnlyAndSameSiteStrict()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var signUpResponse = await PostSignUpAsync(client, new
        {
            email = $"cookie-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Test",
            lastName = "Cookie",
            businessName = "Bodega cookie test",
            businessType = "RETAIL"
        });
        signUpResponse.EnsureSuccessStatusCode();

        Assert.True(signUpResponse.Headers.TryGetValues("Set-Cookie", out var cookieHeaders));
        var sessionCookie = Assert.Single(cookieHeaders!, header => header.StartsWith("bodega_session="));
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", sessionCookie, StringComparison.OrdinalIgnoreCase);
        // The test host runs as Development (see BodegaApiFactory) — plain
        // http, so Secure must be off here or no browser could ever send the
        // cookie back over localhost. Production sets it (see
        // AuthenticationController.SetSessionCookie).
        Assert.DoesNotContain("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);

        // Manually replaying just the cookie (no Authorization header at all)
        // proves it alone is enough to authenticate — not relying on the
        // client's own cookie jar, which HandleCookies=false just disabled.
        var cookieValue = sessionCookie[..sessionCookie.IndexOf(';')];
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("Cookie", cookieValue);
        var protectedResponse = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    /// <summary>
    ///     Sign-out has to do two things at once: clear the cookie for the
    ///     browser that called it, and make the token itself stop working
    ///     everywhere — otherwise a copy of the token grabbed before "logout"
    ///     (header, another tab, wherever) would keep working until it
    ///     naturally expired, exactly the gap that existed before this change
    ///     (sign-out used to be purely client-side, no backend call at all).
    /// </summary>
    [Fact]
    public async Task SignOut_ClearsTheCookie_AndInvalidatesTheTokenEverywhere()
    {
        var client = _factory.CreateClient();

        var signUpResponse = await PostSignUpAsync(client, new
        {
            email = $"cookie-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Test",
            lastName = "Cookie",
            businessName = "Bodega cookie test 2",
            businessType = "RETAIL"
        });
        signUpResponse.EnsureSuccessStatusCode();
        var token = (await ReadJsonAsync(signUpResponse)).GetProperty("token").GetString()!;

        // A completely separate client, authenticated purely by header —
        // stands in for "another device/tab holding a copy of this token".
        var headerClient = AuthenticatedClient(token);
        Assert.Equal(HttpStatusCode.OK, (await headerClient.GetAsync("/api/v1/products")).StatusCode);

        var signOutResponse = await client.PostAsync("/api/v1/authentication/sign-out", null);
        Assert.Equal(HttpStatusCode.NoContent, signOutResponse.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await headerClient.GetAsync("/api/v1/products")).StatusCode);
    }
}
