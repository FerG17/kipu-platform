using System.Collections.Concurrent;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Tests.Infrastructure;

/// <summary>
///     Replaces the real email service in the test host (see
///     KipuApiFactory) so password-reset tests can read the code that
///     "arrived" without ever touching SendGrid.
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
}
