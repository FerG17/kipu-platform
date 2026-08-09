namespace Bodega.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Creates a team member for an already-existing business (the "Invite
///     user" modal) — unlike SignUpCommand, no new Business is created here.
/// </summary>
public record InviteUserCommand(
    string Email,
    string Password,
    string Name,
    string LastName,
    int BusinessId,
    int RoleId,
    string Phone);
