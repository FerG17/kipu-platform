using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Iam.Infrastructure.Email.Logging.Services;

/// <summary>
///     Stand-in used whenever SendGrid:ApiKey isn't configured — local dev
///     without a key yet, and every automated test — so the reset flow is
///     fully exercisable without a real email account. Logs the code instead
///     of sending it.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendPasswordResetCodeAsync(string toEmail, string toName, string code,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "SendGrid is not configured — password-reset code for {Email} was NOT emailed. Code: {Code}",
            toEmail, code);
        return Task.CompletedTask;
    }
}
