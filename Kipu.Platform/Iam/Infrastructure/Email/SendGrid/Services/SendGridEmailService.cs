using Microsoft.Extensions.Options;
using global::SendGrid;
using global::SendGrid.Helpers.Mail;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Infrastructure.Email.SendGrid.Configuration;

namespace Kipu.Platform.Iam.Infrastructure.Email.SendGrid.Services;

public class SendGridEmailService(IOptions<SendGridSettings> settings, ILogger<SendGridEmailService> logger) : IEmailService
{
    public async Task SendPasswordResetCodeAsync(string toEmail, string toName, string code,
        CancellationToken cancellationToken = default)
    {
        var client = new SendGridClient(settings.Value.ApiKey);
        var from = new EmailAddress(settings.Value.FromEmail, settings.Value.FromName);
        var to = new EmailAddress(toEmail, toName);

        var message = MailHelper.CreateSingleEmail(from, to,
            subject: "Tu código para restablecer tu contraseña en Kipu",
            plainTextContent: $"Tu código para restablecer tu contraseña es: {code}\n\nVence en 5 minutos. Si no lo pediste, ignora este correo.",
            htmlContent: $"<p>Tu código para restablecer tu contraseña es:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:4px;\">{code}</p><p>Vence en 5 minutos. Si no lo pediste, ignora este correo.</p>");

        var response = await client.SendEmailAsync(message, cancellationToken);

        // SendGrid accepts the request (2xx) without confirming actual
        // delivery — this only catches outright rejection (bad API key,
        // unverified sender, malformed address), which would otherwise fail
        // silently and leave the user waiting on a code that never sends.
        if ((int)response.StatusCode >= 300)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            logger.LogError("SendGrid rejected the password-reset email for {Email}: {StatusCode} {Body}",
                toEmail, response.StatusCode, body);
        }
    }
}
