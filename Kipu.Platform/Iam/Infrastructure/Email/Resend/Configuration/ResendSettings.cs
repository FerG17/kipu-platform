namespace Kipu.Platform.Iam.Infrastructure.Email.Resend.Configuration;

public class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Kipu";
}
