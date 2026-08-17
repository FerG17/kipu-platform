using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     Forgot-password: email → 6-digit code → verify → set a new password.
///     The email itself is captured by CapturingEmailService (see
///     KipuApiFactory) instead of actually going to Resend.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class PasswordResetTests(KipuApiFactory factory) : IntegrationTestBase(factory)
{
    private string CodeSentTo(string email) =>
        factory.Services.GetRequiredService<CapturingEmailService>().LastCodeFor(email)
        ?? throw new InvalidOperationException($"No reset code was captured for {email}");

    [Fact]
    public async Task FullFlow_RequestVerifyReset_SignsInWithTheNewPassword()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();

        var code = CodeSentTo(admin.Email);

        var verify = await Client.PostAsJsonAsync("/api/v1/authentication/verify-reset-code",
            new { email = admin.Email, code });
        Assert.True(verify.IsSuccessStatusCode);

        const string newPassword = "BrandNewPassw0rd!";
        var reset = await Client.PostAsJsonAsync("/api/v1/authentication/reset-password",
            new { email = admin.Email, code, newPassword });
        Assert.True(reset.IsSuccessStatusCode);

        // Old password no longer works, new one does.
        var oldSignIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = admin.Email, password = ValidPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldSignIn.StatusCode);

        var newSignIn = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = admin.Email, password = newPassword });
        Assert.True(newSignIn.IsSuccessStatusCode);
    }

    /// <summary>An unknown email must look identical to a known one — same 200, nothing to tell them apart.</summary>
    [Fact]
    public async Task RequestingACodeForAnUnknownEmail_StillReturns200()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password",
            new { email = $"nobody-{Guid.NewGuid():N}@test.local" });

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task WrongCode_IsRejected_AndDoesNotResetThePassword()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();

        var verify = await Client.PostAsJsonAsync("/api/v1/authentication/verify-reset-code",
            new { email = admin.Email, code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);

        var reset = await Client.PostAsJsonAsync("/api/v1/authentication/reset-password",
            new { email = admin.Email, code = "000000", newPassword = "BrandNewPassw0rd!" });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);

        // The real password is untouched.
        var stillWorks = await Client.PostAsJsonAsync("/api/v1/authentication/sign-in",
            new { email = admin.Email, password = ValidPassword });
        Assert.True(stillWorks.IsSuccessStatusCode);
    }

    /// <summary>Reset must not succeed on a code that was never run through verify-reset-code first.</summary>
    [Fact]
    public async Task ResetPassword_WithoutVerifyingFirst_IsRejected()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();

        var code = CodeSentTo(admin.Email);

        var reset = await Client.PostAsJsonAsync("/api/v1/authentication/reset-password",
            new { email = admin.Email, code, newPassword = "BrandNewPassw0rd!" });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    /// <summary>5 wrong guesses burns the code even if the 6th guess would have been right.</summary>
    [Fact]
    public async Task TooManyWrongAttempts_InvalidatesTheCode()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();

        var code = CodeSentTo(admin.Email);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await Client.PostAsJsonAsync("/api/v1/authentication/verify-reset-code",
                new { email = admin.Email, code = "000000" });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        }

        // The real code is now dead too, even though it was never guessed wrong itself.
        var verify = await Client.PostAsJsonAsync("/api/v1/authentication/verify-reset-code",
            new { email = admin.Email, code });
        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
    }

    /// <summary>Requesting a second code kills the first one — never more than one guessable code at a time.</summary>
    [Fact]
    public async Task RequestingANewCode_InvalidatesThePreviousOne()
    {
        var admin = await CreateBusinessWithOwnerAsync();

        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();
        var firstCode = CodeSentTo(admin.Email);

        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email = admin.Email }))
            .EnsureSuccessStatusCode();

        var verifyOld = await Client.PostAsJsonAsync("/api/v1/authentication/verify-reset-code",
            new { email = admin.Email, code = firstCode });
        Assert.Equal(HttpStatusCode.BadRequest, verifyOld.StatusCode);
    }

    /// <summary>A suspended employee can't use a password reset to sidestep being suspended.</summary>
    [Fact]
    public async Task SuspendedUser_CannotRequestAResetCode()
    {
        var admin = await CreateBusinessWithOwnerAsync();
        var email = await InviteMemberAsync(admin.Client, CashierRoleId);
        var users = await ReadJsonAsync(await admin.Client.GetAsync("/api/v1/users"));
        var memberId = users.EnumerateArray().First(u => u.GetProperty("email").GetString() == email)
            .GetProperty("id").GetInt32();
        (await admin.Client.PatchAsync($"/api/v1/users/{memberId}/deactivate", null)).EnsureSuccessStatusCode();

        (await Client.PostAsJsonAsync("/api/v1/authentication/forgot-password", new { email }))
            .EnsureSuccessStatusCode();

        Assert.Null(factory.Services.GetRequiredService<CapturingEmailService>().LastCodeFor(email));
    }
}
