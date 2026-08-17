namespace Kipu.Platform.Iam.Domain.Model.Commands;

public record VerifyPasswordResetCodeCommand(string Email, string Code);
