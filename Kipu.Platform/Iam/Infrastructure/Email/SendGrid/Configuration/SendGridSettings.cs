namespace Kipu.Platform.Iam.Infrastructure.Email.SendGrid.Configuration;

public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Kipu";
}
