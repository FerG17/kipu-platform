using System.Net.Http.Json;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     The "last active admin" guard (see UserCommandService.IsLastActiveAdminAsync)
///     reads the rest of the team before deciding whether removing/suspending
///     one more admin would leave zero. Two different admins racing to
///     deactivate each other is the two-different-rows version of the same
///     race StockConcurrencyTests covers for a single row: each read can see
///     "the other one is still active" before either write commits, so
///     without something stronger than a plain read-then-write, both could
///     succeed and leave the business with no one who can sign in and run it.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class IamConcurrencyTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private static async Task<int> UserIdByEmailAsync(HttpClient adminClient, string email)
    {
        var users = await ReadJsonAsync(await adminClient.GetAsync("/api/v1/users"));
        return users.EnumerateArray().First(u => u.GetProperty("email").GetString() == email)
            .GetProperty("id").GetInt32();
    }

    /// <summary>
    ///     Exactly one of the two concurrent deactivations may succeed — the
    ///     business must end this test with at least one active admin, never
    ///     zero and never a 500 for the loser.
    /// </summary>
    [Fact]
    public async Task ConcurrentDeactivationsOfTheLastTwoAdmins_SucceedOnlyOnce()
    {
        var owner = await CreateBusinessWithOwnerAsync();
        var secondAdminEmail = $"admin2-{Guid.NewGuid():N}@test.local";
        (await owner.Client.PostAsJsonAsync("/api/v1/users", new
        {
            email = secondAdminEmail,
            password = ValidPassword,
            name = "Second",
            lastName = "Admin",
            roleId = AdminRoleId,
            phone = ""
        })).EnsureSuccessStatusCode();

        var secondAdminId = await UserIdByEmailAsync(owner.Client, secondAdminEmail);
        var secondAdminToken = await SignInForTokenAsync(secondAdminEmail, ValidPassword);
        var secondAdminClient = AuthenticatedClient(secondAdminToken);

        // Owner tries to deactivate the second admin; the second admin tries
        // to deactivate the owner — at the same instant, each still seeing
        // the other as the "at least one other admin survives" answer.
        var attempts = await Task.WhenAll(
            owner.Client.PatchAsync($"/api/v1/users/{secondAdminId}/deactivate", null),
            secondAdminClient.PatchAsync($"/api/v1/users/{owner.UserId}/deactivate", null));

        var succeeded = attempts.Count(response => response.IsSuccessStatusCode);
        Assert.Equal(1, succeeded);
        Assert.All(attempts, response => Assert.True(
            response.IsSuccessStatusCode || (int)response.StatusCode == 409,
            $"expected success or 409, got {(int)response.StatusCode}"));

        // Deactivating bumps TokenVersion, so whichever admin lost their own
        // access can no longer call the API — read the final state through
        // whichever client's own request just succeeded.
        var survivingClient = attempts[0].IsSuccessStatusCode ? owner.Client : secondAdminClient;
        var remainingAdmins = (await ReadJsonAsync(await survivingClient.GetAsync("/api/v1/users")))
            .EnumerateArray()
            .Count(u => u.GetProperty("roleId").GetInt32() == AdminRoleId && u.GetProperty("status").GetString() == "ACTIVE");
        Assert.True(remainingAdmins >= 1, "the business was left with zero active admins");
    }
}
