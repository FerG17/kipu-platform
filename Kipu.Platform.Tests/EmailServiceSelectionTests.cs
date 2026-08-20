using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Infrastructure.Email.Logging.Services;
using Kipu.Platform.Iam.Infrastructure.Email.Resend.Services;
using Kipu.Platform.Tests.Infrastructure;

namespace Kipu.Platform.Tests;

/// <summary>
///     X4 A1/A2: which IEmailService Program.cs registers depends on whether
///     Resend:ApiKey looks like a real key after
///     Environment.ExpandEnvironmentVariables — these boot a bare
///     WebApplicationFactory&lt;Program&gt; (not KipuApiFactory, which always
///     overrides IEmailService with CapturingEmailService regardless of what
///     Program.cs picked) so the real registration can be inspected.
/// </summary>
[Collection(KipuApiCollection.Name)]
public class EmailServiceSelectionTests
{
    private const string ApiKeyEnvVar = "Resend__ApiKey";

    /// <summary>
    ///     A2: RESEND_API_KEY not set in the real environment means
    ///     ExpandEnvironmentVariables leaves the literal "%RESEND_API_KEY%" in
    ///     Resend:ApiKey — appsettings.json's own default, reproduced here by
    ///     just not overriding it. That literal string is not blank, so before
    ///     the fix it registered ResendEmailService anyway, with a garbage key
    ///     Resend would reject on every real send. It must fall back to the
    ///     logging stand-in instead.
    /// </summary>
    [Fact]
    public async Task UnexpandedApiKeyPlaceholder_FallsBackToLoggingEmailService()
    {
        var original = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        Environment.SetEnvironmentVariable(ApiKeyEnvVar, "%RESEND_API_KEY%");
        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            Assert.IsType<LoggingEmailService>(emailService);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApiKeyEnvVar, original);
        }
    }

    /// <summary>A real-looking key must still register the real service — the guard only rejects the literal placeholder pattern, not any non-empty value.</summary>
    [Fact]
    public async Task RealLookingApiKey_RegistersResendEmailService()
    {
        var original = Environment.GetEnvironmentVariable(ApiKeyEnvVar);
        Environment.SetEnvironmentVariable(ApiKeyEnvVar, "re_1234567890abcdefghijklmnop");
        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            Assert.IsType<ResendEmailService>(emailService);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApiKeyEnvVar, original);
        }
    }
}
