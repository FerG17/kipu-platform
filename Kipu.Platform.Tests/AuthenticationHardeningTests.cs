using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Kipu.Platform.Tests.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace Kipu.Platform.Tests;

/// <summary>
///     Attacks the bearer-token layer itself rather than the endpoints behind
///     it: forging a token, swapping the signature algorithm, editing the
///     business_id claim, replaying a token after the password changed.
///
///     A single weakness here defeats every other control in the system at
///     once — the JWT is the only thing that says who the caller is and which
///     bodega's data they may touch — so each classic JWT attack gets its own
///     test instead of being assumed impossible.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class AuthenticationHardeningTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    /// <summary>Mirrors KipuApiFactory's test signing key — the one the app really uses in this suite.</summary>
    private const string RealSigningKey = "integration-test-signing-key-not-used-anywhere-else-0123456789";

    private const string AttackerSigningKey = "attacker-controlled-signing-key-0123456789-abcdefghijklmnop";

    /// <summary>A protected endpoint every authenticated role may call — the canary for "did this token work".</summary>
    private const string ProtectedEndpoint = "/api/v1/products";

    [Fact]
    public async Task Request_WithoutAnyToken_IsRejected()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    [Theory]
    [InlineData("not-a-jwt-at-all")]
    [InlineData("a.b.c")]
    [InlineData("")]
    public async Task Request_WithAMalformedToken_IsRejected(string token)
    {
        var client = AuthenticatedClient(token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>
    ///     The core forgery attempt: a structurally perfect token, correct
    ///     claims, signed with a key the attacker picked. It must not be
    ///     accepted — if it is, anyone can mint an admin token for any bodega.
    /// </summary>
    [Fact]
    public async Task Token_SignedWithAnAttackerKey_IsRejected()
    {
        var victim = await CreateBusinessWithOwnerAsync();

        var forged = BuildToken(AttackerSigningKey, victim.UserId, victim.BusinessId, "ADMIN", tokenVersion: 0);
        var client = AuthenticatedClient(forged);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>An unsigned "alg: none" token — the oldest JWT bypass there is.</summary>
    [Fact]
    public async Task Token_WithAlgNone_IsRejected()
    {
        var victim = await CreateBusinessWithOwnerAsync();

        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var payload = Base64Url($$"""
            {"nameid":"{{victim.UserId}}","business_id":"{{victim.BusinessId}}","role":"ADMIN","token_version":"0","exp":{{DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()}}}
            """);

        var client = AuthenticatedClient($"{header}.{payload}.");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>
    ///     Takes a genuine token and edits only the business_id claim, then
    ///     re-signs with the attacker's key. This is the "become another
    ///     tenant" move; the signature check is what has to stop it.
    /// </summary>
    [Fact]
    public async Task Token_WithATamperedBusinessIdClaim_IsRejected()
    {
        var victim = await CreateBusinessWithOwnerAsync();
        var attacker = await CreateBusinessWithOwnerAsync();

        var tampered = BuildToken(AttackerSigningKey, attacker.UserId, victim.BusinessId, "ADMIN", tokenVersion: 0);
        var client = AuthenticatedClient(tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>
    ///     Even correctly signed, a token naming a user that doesn't exist (or
    ///     a stale token_version) must not pass — the middleware re-checks the
    ///     user against the database on every request.
    /// </summary>
    [Fact]
    public async Task Token_WithAStaleTokenVersion_IsRejected()
    {
        var victim = await CreateBusinessWithOwnerAsync();

        var stale = BuildToken(RealSigningKey, victim.UserId, victim.BusinessId, "ADMIN", tokenVersion: 999);
        var client = AuthenticatedClient(stale);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    [Fact]
    public async Task Token_ForANonexistentUser_IsRejected()
    {
        var forged = BuildToken(RealSigningKey, userId: 999_999_999, businessId: 1, role: "ADMIN", tokenVersion: 0);
        var client = AuthenticatedClient(forged);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>An expired token must be refused even though its signature is perfectly valid.</summary>
    [Fact]
    public async Task Token_ThatHasExpired_IsRejected()
    {
        var victim = await CreateBusinessWithOwnerAsync();

        var expired = BuildToken(RealSigningKey, victim.UserId, victim.BusinessId, "ADMIN", tokenVersion: 0,
            expires: DateTime.UtcNow.AddMinutes(-5));
        var client = AuthenticatedClient(expired);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>
    ///     Changing the password has to revoke tokens issued before it —
    ///     otherwise "someone has my password, I'll change it" doesn't
    ///     actually evict them.
    /// </summary>
    [Fact]
    public async Task ChangingThePassword_RevokesPreviouslyIssuedTokens()
    {
        var owner = await CreateBusinessWithOwnerAsync();
        var stolenClient = AuthenticatedClient(owner.Token);

        // The stolen token works right up until the password changes.
        (await stolenClient.GetAsync(ProtectedEndpoint)).EnsureSuccessStatusCode();

        var change = await owner.Client.PostAsJsonAsync($"/api/v1/users/{owner.UserId}/change-password", new
        {
            currentPassword = ValidPassword,
            newPassword = "N3wPassw0rd!"
        });
        change.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await stolenClient.GetAsync(ProtectedEndpoint)).StatusCode);
    }

    /// <summary>Changing a password must require knowing the old one, or an open session becomes a full takeover.</summary>
    [Fact]
    public async Task ChangingThePassword_WithoutTheCurrentOne_IsRejected()
    {
        var owner = await CreateBusinessWithOwnerAsync();

        var response = await owner.Client.PostAsJsonAsync($"/api/v1/users/{owner.UserId}/change-password", new
        {
            currentPassword = "definitely-not-the-password",
            newPassword = "N3wPassw0rd!"
        });

        Assert.False(response.IsSuccessStatusCode, "a password change without the current password must be refused");
    }

    /// <summary>
    ///     A removed account must not be able to authenticate any more, and
    ///     any token it already holds must stop working immediately — being
    ///     let go from the bodega has to actually end the session.
    /// </summary>
    [Fact]
    public async Task RemovedUser_CannotSignInAndTheirExistingTokenStopsWorking()
    {
        var admin = await CreateBusinessAsync();
        var memberEmail = await InviteMemberAsync(admin, CashierRoleId);

        var memberToken = await SignInForTokenAsync(memberEmail, ValidPassword);
        var memberClient = AuthenticatedClient(memberToken);
        (await memberClient.GetAsync(ProtectedEndpoint)).EnsureSuccessStatusCode();

        var members = await admin.GetAsync("/api/v1/users");
        members.EnsureSuccessStatusCode();
        var memberId = (await ReadJsonAsync(members)).EnumerateArray()
            .First(user => user.GetProperty("email").GetString() == memberEmail)
            .GetProperty("id").GetInt32();

        (await admin.DeleteAsync($"/api/v1/users/{memberId}")).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await memberClient.GetAsync(ProtectedEndpoint)).StatusCode);

        var signIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = memberEmail, password = ValidPassword });

        Assert.False(signIn.IsSuccessStatusCode,
            "a removed user must not be able to sign in, got " + (int)signIn.StatusCode);
    }

    /// <summary>
    ///     A business must always keep someone who can administer it. Deleting
    ///     the only admin — including deleting yourself, which the endpoint
    ///     allowed — is unrecoverable: there is no password reset and no
    ///     support path, so the bodega's products, sales and credit would stay
    ///     in the database with nobody able to reach them ever again.
    /// </summary>
    [Fact]
    public async Task TheLastAdministrator_CannotBeRemoved()
    {
        var owner = await CreateBusinessWithOwnerAsync();

        var response = await owner.Client.DeleteAsync($"/api/v1/users/{owner.UserId}");

        Assert.False(response.IsSuccessStatusCode,
            $"deleting the only administrator must be refused, got {(int)response.StatusCode}");

        // And the account still works afterwards.
        (await owner.Client.GetAsync(ProtectedEndpoint)).EnsureSuccessStatusCode();
    }

    /// <summary>Once a second admin exists, stepping down is legitimate again.</summary>
    [Fact]
    public async Task AnAdministrator_CanBeRemovedOnceAnotherOneExists()
    {
        var owner = await CreateBusinessWithOwnerAsync();
        var secondAdminEmail = await InviteMemberAsync(owner.Client, AdminRoleId);

        var team = await owner.Client.GetAsync("/api/v1/users");
        team.EnsureSuccessStatusCode();
        var secondAdminId = (await ReadJsonAsync(team)).EnumerateArray()
            .First(user => user.GetProperty("email").GetString() == secondAdminEmail)
            .GetProperty("id").GetInt32();

        (await owner.Client.DeleteAsync($"/api/v1/users/{secondAdminId}")).EnsureSuccessStatusCode();
    }

    /// <summary>Wrong credentials must never say which half was wrong — that turns sign-in into a user-enumeration oracle.</summary>
    [Fact]
    public async Task SignIn_WithWrongCredentials_DoesNotRevealWhetherTheEmailExists()
    {
        var owner = await CreateBusinessWithOwnerAsync();

        var wrongPassword = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = owner.Email, password = "wrong-password-entirely" });
        var unknownEmail = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = $"nobody-{Guid.NewGuid():N}@test.local", password = "wrong-password-entirely" });

        Assert.Equal(unknownEmail.StatusCode, wrongPassword.StatusCode);
        Assert.Equal(await unknownEmail.Content.ReadAsStringAsync(), await wrongPassword.Content.ReadAsStringAsync());
    }

    /// <summary>
    ///     Public sign-up is closed — see AuthenticationController.SignUp.
    ///     Without the platform-admin bootstrap key, nobody can create a
    ///     second business through this endpoint at all, key or no key on the
    ///     rest of the payload.
    /// </summary>
    [Fact]
    public async Task SignUp_WithoutTheBootstrapKey_IsRejected()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/sign-up", new
        {
            email = $"nokey-{Guid.NewGuid():N}@test.local",
            password = ValidPassword,
            name = "Test",
            lastName = "NoKey",
            businessName = "Kipu sin llave",
            businessType = "RETAIL"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>A guessed or stale key must fail exactly like no key at all.</summary>
    [Fact]
    public async Task SignUp_WithTheWrongBootstrapKey_IsRejected()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authentication/sign-up")
        {
            Content = JsonContent.Create(new
            {
                email = $"wrongkey-{Guid.NewGuid():N}@test.local",
                password = ValidPassword,
                name = "Test",
                lastName = "WrongKey",
                businessName = "Kipu llave equivocada",
                businessType = "RETAIL"
            })
        };
        request.Headers.Add("X-Bootstrap-Key", "not-the-real-bootstrap-key-at-all");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Sign-up must not become an account-enumeration or account-takeover path for an existing email.</summary>
    [Fact]
    public async Task SignUp_WithAnAlreadyRegisteredEmail_DoesNotOverwriteTheExistingAccount()
    {
        var owner = await CreateBusinessWithOwnerAsync();

        var response = await PostSignUpAsync(Client, new
        {
            email = owner.Email,
            password = "Attack3rPassword!",
            name = "Mallory",
            lastName = "Attacker",
            businessName = "Kipu falsa",
            businessType = "RETAIL"
        });

        Assert.False(response.IsSuccessStatusCode);

        // The original credentials must still be the ones that work.
        var stillValid = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = owner.Email, password = ValidPassword });
        stillValid.EnsureSuccessStatusCode();
    }

    /// <summary>Weak passwords must be refused at every entry point that sets one.</summary>
    [Theory]
    [InlineData("short1")]
    [InlineData("alllettersnodigits")]
    [InlineData("12345678")]
    public async Task SignUp_WithAWeakPassword_IsRejected(string password)
    {
        var response = await PostSignUpAsync(Client, new
        {
            email = $"weak-{Guid.NewGuid():N}@test.local",
            password,
            name = "Test",
            lastName = "Weak",
            businessName = "Kipu",
            businessType = "RETAIL"
        });

        Assert.False(response.IsSuccessStatusCode, $"password '{password}' must be refused");
    }

    /// <summary>The password hash must never appear in any response body.</summary>
    [Fact]
    public async Task UserResponses_NeverIncludeThePasswordHash()
    {
        var owner = await CreateBusinessWithOwnerAsync();

        foreach (var path in new[] { $"/api/v1/users/{owner.UserId}", "/api/v1/users" })
        {
            var response = await owner.Client.GetAsync(path);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("$2a$", body, StringComparison.Ordinal);
        }
    }

    /// <summary>An unhandled failure must never hand a stack trace or SQL back to the caller.</summary>
    [Fact]
    public async Task ErrorResponses_DoNotLeakInternals()
    {
        var client = await CreateBusinessAsync();

        var response = await client.GetAsync("/api/v1/products/2147483647");
        var body = await response.Content.ReadAsStringAsync();

        foreach (var leak in new[] { "StackTrace", "at Kipu.Platform", "MySql", "SELECT ", "Microsoft.EntityFrameworkCore" })
            Assert.DoesNotContain(leak, body, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildToken(string signingKey, int userId, int businessId, string role, int tokenVersion,
        DateTime? expires = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(signingKey);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("business_id", businessId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("token_version", tokenVersion.ToString())
            ]),
            // NotBefore has to trail Expires or the handler refuses to build the
            // token at all — an expired token is written as one whose whole
            // validity window is already in the past.
            NotBefore = (expires ?? DateTime.UtcNow.AddDays(1)).AddMinutes(-10),
            Expires = expires ?? DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static string Base64Url(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
