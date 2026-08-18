using System.Collections.Concurrent;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Tests.Infrastructure;

/// <summary>
///     Replaces the real email service in the test host (see
///     KipuApiFactory) so password-reset tests can read the code that
///     "arrived" without ever touching Resend.
/// </summary>
public class CapturingEmailService : IEmailService
{
    private readonly ConcurrentDictionary<string, string> lastCodeByEmail = new();

    public Task SendPasswordResetCodeAsync(string toEmail, string toName, string code,
        CancellationToken cancellationToken = default)
    {
        lastCodeByEmail[toEmail] = code;
        return Task.CompletedTask;
    }

    public string? LastCodeFor(string email) => lastCodeByEmail.GetValueOrDefault(email);

    /// <summary>
    ///     RequestPasswordResetCommand sends the email in the background now
    ///     (fire-and-forget, not awaited before the HTTP response — see its
    ///     doc comment) specifically so response timing can't reveal whether
    ///     an account exists. That means the code isn't guaranteed captured
    ///     the instant the request completes; tests poll briefly instead of
    ///     assuming it is.
    /// </summary>
    public async Task<string> WaitForCodeAsync(string email, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        while (DateTime.UtcNow < deadline)
        {
            if (lastCodeByEmail.TryGetValue(email, out var code)) return code;
            await Task.Delay(10);
        }

        throw new TimeoutException($"No reset code arrived for {email} within the timeout.");
    }
}
