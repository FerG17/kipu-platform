namespace Kipu.Platform.Iam.Application.Internal.OutboundServices;

public interface IEmailService
{
    /// <summary>Sends the 6-digit password-reset code to the user's own email.</summary>
    Task SendPasswordResetCodeAsync(string toEmail, string toName, string code, CancellationToken cancellationToken = default);
}
