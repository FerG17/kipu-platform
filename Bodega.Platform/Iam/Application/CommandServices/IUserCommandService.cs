using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Iam.Application.CommandServices;

public interface IUserCommandService
{
    Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken);

    /// <summary>Also returns a token, matching the frontend's expectation of being auto-logged-in right after registering.</summary>
    Task<Result<(User user, string token)>> Handle(SignUpCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(InviteUserCommand command, CancellationToken cancellationToken);
    Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken);

    /// <summary>Invalidates every token issued for this user so far — see User.RevokeAllSessions.</summary>
    Task<Result> Handle(SignOutCommand command, CancellationToken cancellationToken);
}
