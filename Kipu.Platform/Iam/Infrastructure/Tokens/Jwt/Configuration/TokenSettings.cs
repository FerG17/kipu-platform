namespace Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;

public class TokenSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpirationDays { get; set; } = 7;
}
