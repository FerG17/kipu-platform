using System.Net;
using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Suspending (and un-suspending) a team member without deleting them —
///     PATCH /users/{id}/deactivate and /reactivate.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class TeamManagementTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Admin_CanDeactivateThenReactivateATeamMember()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var email = await InviteMemberAsync(admin.Client, CashierRoleId);

        // Member can sign in before anything happens.
        await SignInForTokenAsync(email, ValidPassword);

        var users = await ReadJsonAsync(await admin.Client.GetAsync("/api/v1/users"));
        var memberId = users.EnumerateArray().First(u => u.GetProperty("email").GetString() == email)
            .GetProperty("id").GetInt32();

        var deactivateResponse = await admin.Client.PatchAsync($"/api/v1/users/{memberId}/deactivate", null);
        Assert.True(deactivateResponse.IsSuccessStatusCode,
            $"Deactivate failed: {(int)deactivateResponse.StatusCode} {await deactivateResponse.Content.ReadAsStringAsync()}");

        // Suspended: sign-in must now be rejected.
        var blockedSignIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { email, password = ValidPassword });
        Assert.False(blockedSignIn.IsSuccessStatusCode,
            "A deactivated user should not be able to sign in");

        var reactivateResponse = await admin.Client.PatchAsync($"/api/v1/users/{memberId}/reactivate", null);
        Assert.True(reactivateResponse.IsSuccessStatusCode,
            $"Reactivate failed: {(int)reactivateResponse.StatusCode} {await reactivateResponse.Content.ReadAsStringAsync()}");

        // Reactivated: sign-in must work again.
        var restoredSignIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { email, password = ValidPassword });
        Assert.True(restoredSignIn.IsSuccessStatusCode,
            $"Reactivated user could not sign in: {(int)restoredSignIn.StatusCode} {await restoredSignIn.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Admin_CannotDeactivateTheLastActiveAdmin()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        var response = await admin.Client.PatchAsync($"/api/v1/users/{admin.UserId}/deactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    ///     Deactivating bumps TokenVersion, which kills the caller's own
    ///     session the instant it runs — an admin who suspends themselves
    ///     would have no token left to call ReactivateUserCommand with, and
    ///     unlike the last-admin rule, this is reachable even with 2+ admins.
    /// </summary>
    [Fact]
    public async Task Admin_CannotDeactivateOrDeleteTheirOwnAccount()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        // A second admin exists so this isn't merely re-testing the
        // last-active-admin rule above.
        await InviteMemberAsync(admin.Client, AdminRoleId);

        var deactivateResponse = await admin.Client.PatchAsync($"/api/v1/users/{admin.UserId}/deactivate", null);
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);

        var deleteResponse = await admin.Client.DeleteAsync($"/api/v1/users/{admin.UserId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        // Untouched: the admin's own session and status are still intact.
        var signIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in", new { email = admin.Email, password = ValidPassword });
        Assert.True(signIn.IsSuccessStatusCode);
    }

    /// <summary>
    ///     X3 minor item: emails weren't trimmed/lowercased before being
    ///     stored, so " Test@Example.com" and "test@example.com" were treated
    ///     as two different accounts instead of one.
    /// </summary>
    [Fact]
    public async Task InvitingAUser_NormalizesTheEmail_TrimAndLowercase()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var rawEmail = $"  Member-{Guid.NewGuid():N}@Test.LOCAL  ";
        var normalizedEmail = rawEmail.Trim().ToLowerInvariant();

        (await admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            email = rawEmail,
            password = ValidPassword,
            name = "Team",
            lastName = "Member",
            roleId = CashierRoleId,
            phone = ""
        })).EnsureSuccessStatusCode();

        // Signing in with the normalized (trimmed/lowercased) form must work.
        var signIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = normalizedEmail, password = ValidPassword });
        Assert.True(signIn.IsSuccessStatusCode);

        // Inviting the same address again — even in a different case/with
        // whitespace — must be rejected as already taken, not create a
        // second account.
        var duplicate = await admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            email = rawEmail.ToUpperInvariant(),
            password = ValidPassword,
            name = "Duplicate",
            lastName = "Member",
            roleId = CashierRoleId,
            phone = ""
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }
}
