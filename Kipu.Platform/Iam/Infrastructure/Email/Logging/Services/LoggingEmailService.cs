using Kipu.Platform.Iam.Application.Internal.OutboundServices;

namespace Kipu.Platform.Iam.Infrastructure.Email.Logging.Services;

/// <summary>
///     Stand-in used whenever Resend:ApiKey isn't configured — local dev
///     without a key yet, and every automated test — so the reset flow is
///     fully exercisable without a real email account. Logs the code instead
///     of sending it.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger, IHostEnvironment environment) : IEmailService
{
    public Task SendPasswordResetCodeAsync(string toEmail, string toName, string code,
        CancellationToken cancellationToken = default)
    {
        // The code itself only belongs in this log line in Development/Testing,
        // where it's the whole point (no real email account to read it from).
        // If Resend ends up misconfigured in Production (RESEND_API_KEY left
        // empty, say), this class becomes the live fallback there too, and a
        // plaintext reset code sitting in Railway's log stream at Warning
        // level would let anyone with log access take over any account.
        if (environment.IsProduction())
            logger.LogWarning(
                "Resend is not configured — password-reset code for {Email} was NOT emailed.", toEmail);
        else
            logger.LogWarning(
                "Resend is not configured — password-reset code for {Email} was NOT emailed. Code: {Code}",
                toEmail, code);
        return Task.CompletedTask;
    }
}
