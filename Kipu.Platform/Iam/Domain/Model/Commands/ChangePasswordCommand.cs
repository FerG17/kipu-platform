namespace Kipu.Platform.Iam.Domain.Model.Commands;

public record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword);
