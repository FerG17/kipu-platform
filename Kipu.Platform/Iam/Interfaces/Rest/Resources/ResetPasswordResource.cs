namespace Kipu.Platform.Iam.Interfaces.Rest.Resources;

public record ResetPasswordResource(string Email, string Code, string NewPassword);
