namespace Kipu.Platform.Iam.Domain.Model.Commands;

public record ResetPasswordCommand(string Email, string Code, string NewPassword);
