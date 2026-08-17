namespace Kipu.Platform.Iam.Domain.Model.Commands;

public record DeactivateUserCommand(int UserId, int ActingUserId);
