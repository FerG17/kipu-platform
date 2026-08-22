namespace Kipu.Platform.Iam.Domain.Model.Commands;

public record DeleteUserCommand(int UserId, int ActingUserId);
