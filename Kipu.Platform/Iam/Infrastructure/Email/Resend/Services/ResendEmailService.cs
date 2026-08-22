using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Infrastructure.Email.Resend.Configuration;

namespace Kipu.Platform.Iam.Infrastructure.Email.Resend.Services;

public class ResendEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<ResendSettings> settings,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private const string ApiUrl = "https://api.resend.com/emails";

    public async Task SendPasswordResetCodeAsync(string toEmail, string toName, string code,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);

        var payload = new
        {
            from = $"{settings.Value.FromName} <{settings.Value.FromEmail}>",
            to = new[] { toEmail },
            subject = "Tu código para restablecer tu contraseña en Kipu",
            html = $"<p>Tu código para restablecer tu contraseña es:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:4px;\">{code}</p><p>Vence en 5 minutos. Si no lo pediste, ignora este correo.</p>",
            text = $"Tu código para restablecer tu contraseña es: {code}\n\nVence en 5 minutos. Si no lo pediste, ignora este correo."
        };

        var response = await client.PostAsJsonAsync(ApiUrl, payload, cancellationToken);

        // Resend accepts the request (2xx) without confirming actual delivery —
        // this only catches outright rejection (bad API key, unverified domain,
        // malformed address), which would otherwise fail silently and leave the
        // user waiting on a code that never sends.
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Resend rejected the password-reset email for {Email}: {StatusCode} {Body}",
                toEmail, response.StatusCode, body);
        }
    }
}
