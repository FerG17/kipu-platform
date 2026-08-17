using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Iam.Application.CommandServices;

public interface IUserCommandService
{
    Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken);

    /// <summary>Also returns a token, matching the frontend's expectation of being auto-logged-in right after registering.</summary>
    Task<Result<(User user, string token)>> Handle(SignUpCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(InviteUserCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken);

    /// <summary>Suspends sign-in access without deleting the account, preserving its audit trail (past sales, movements, etc).</summary>
    Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken);

    /// <summary>Restores sign-in access for a previously suspended user.</summary>
    Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken);

    /// <summary>Emails a 6-digit code if the address belongs to an active user — always succeeds either way, so the response can't be used to test which emails are registered.</summary>
    Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken);

    /// <summary>Checks a code without consuming it — lets the UI confirm it before asking for a new password.</summary>
    Task<Result> Handle(VerifyPasswordResetCodeCommand command, CancellationToken cancellationToken);

    /// <summary>Only succeeds against a code already confirmed via VerifyPasswordResetCodeCommand — sets the new password and signs out every other session.</summary>
    Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken);

    /// <summary>Invalidates every token issued for this user so far — see User.RevokeAllSessions.</summary>
    Task<Result> Handle(SignOutCommand command, CancellationToken cancellationToken);
}
