using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Domain.Model.Errors;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Iam.Resources;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Handles every User-related command, including the atomic sign-up flow
///     (see Handle(SignUpCommand)).
/// </summary>
public class UserCommandService(
    IUserRepository userRepository,
    IBusinessRepository businessRepository,
    IRoleRepository roleRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    IProductContextFacade productContextFacade,
    IValidator<SignUpCommand> signUpValidator,
    IValidator<InviteUserCommand> inviteUserValidator,
    IValidator<ChangePasswordCommand> changePasswordValidator,
    IStringLocalizer<IamMessages> localizer)
    : IUserCommandService
{
    private const int DefaultOwnerRoleId = 1; // ADMIN — matches the frontend's hardcoded sign-up roleId.

    public async Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);

        if (user == null || !hashingService.VerifyPassword(command.Password, user.PasswordHash))
            return Result<(User user, string token)>.Failure(IamError.InvalidCredentials,
                localizer[nameof(IamError.InvalidCredentials)]);

        // RequestAuthorizationMiddleware refuses a non-ACTIVE user's token on
        // every request, but sign-in itself never looked at Status — so a
        // suspended account still got a 200 and a token that silently failed
        // everywhere. Answering "invalid credentials" (not "your account is
        // disabled") keeps sign-in from confirming which emails are real.
        if (user.Status != UserStatus.Active)
            return Result<(User user, string token)>.Failure(IamError.InvalidCredentials,
                localizer[nameof(IamError.InvalidCredentials)]);

        var roleName = await ResolveRoleNameAsync(user.RoleId, cancellationToken);
        var token = tokenService.GenerateToken(user, roleName);
        return Result<(User user, string token)>.Success((user, token));
    }

    /// <summary>
    ///     Creates the User and its Business in one atomic operation — the
    ///     historical bug this replaces left businessId permanently null
    ///     because sign-up only ever created the user (see handoff doc §3.1).
    ///
    ///     User.BusinessId and Business.UserId are circular (each references
    ///     the other), and MySQL checks foreign keys immediately per
    ///     statement, not at commit — so neither row can be inserted first if
    ///     both sides were hard FKs. Business.UserId is deliberately not a
    ///     database-enforced FK (see Business.cs / ModelBuilderExtensions),
    ///     which lets us: insert Business first (real Id assigned) → insert
    ///     User with a valid BusinessId → patch Business.UserId in the same
    ///     SaveChanges as the User insert. Both round-trips are wrapped in one
    ///     explicit transaction, so a failure on either one rolls back both —
    ///     no intermediate state is ever observable.
    /// </summary>
    public async Task<Result<(User user, string token)>> Handle(SignUpCommand command, CancellationToken cancellationToken)
    {
        if (!(await signUpValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<(User user, string token)>.Failure(IamError.WeakPassword,
                localizer[nameof(IamError.WeakPassword)]);

        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result<(User user, string token)>.Failure(IamError.EmailAlreadyTaken,
                localizer[nameof(IamError.EmailAlreadyTaken), command.Email]);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var business = new Business(command.BusinessName, command.BusinessType, userId: 0);
            business.UpdateProfile(command.BusinessName, command.BusinessType, command.Address, command.Ruc);
            await businessRepository.AddAsync(business, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken); // business.Id is now populated.

            var hashedPassword = hashingService.HashPassword(command.Password);
            var user = new User(command.Email, hashedPassword, command.Name, command.LastName, business.Id,
                DefaultOwnerRoleId, command.Phone);
            await userRepository.AddAsync(user, cancellationToken);
            business.LinkOwner(0); // placeholder; real user.Id isn't known until the next SaveChanges.

            await unitOfWork.CompleteAsync(cancellationToken); // inserts the user, user.Id is now populated.
            business.LinkOwner(user.Id);
            await unitOfWork.CompleteAsync(cancellationToken); // patches business.UserId with the real owner.

            // Product & Inventory's facade call runs on this same scoped
            // DbContext/transaction — its own internal CompleteAsync call
            // just adds another SaveChanges to the same not-yet-committed
            // transaction, so the default warehouse is created atomically
            // together with the user and the business.
            await productContextFacade.CreateDefaultWarehouse(business.Id, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var roleName = await ResolveRoleNameAsync(user.RoleId, cancellationToken);
            var token = tokenService.GenerateToken(user, roleName);
            return Result<(User user, string token)>.Success((user, token));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<(User user, string token)>.Failure(IamError.DatabaseError,
                localizer[nameof(IamError.DatabaseError)]);
        }
    }

    /// <summary>
    ///     Creates a team member for an already-existing business (no
    ///     circular-FK concern here, unlike sign-up — the business already
    ///     has a real Id).
    /// </summary>
    public async Task<Result<User>> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        if (!(await inviteUserValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<User>.Failure(IamError.WeakPassword, localizer[nameof(IamError.WeakPassword)]);

        // RoleId came straight from the request body into the User row with
        // no check at all. A nonexistent role hit the foreign key and blew up
        // as an unhandled DbUpdateException — a 500 for what is plainly a bad
        // request. Checked against the real catalog rather than a hardcoded
        // list so it stays correct if roles are ever added.
        if (await roleRepository.FindByIdAsync(command.RoleId, cancellationToken) == null)
            return Result<User>.Failure(IamError.RoleNotFound, localizer[nameof(IamError.RoleNotFound)]);

        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result<User>.Failure(IamError.EmailAlreadyTaken,
                localizer[nameof(IamError.EmailAlreadyTaken), command.Email]);

        var hashedPassword = hashingService.HashPassword(command.Password);
        var user = new User(command.Email, hashedPassword, command.Name, command.LastName, command.BusinessId,
            command.RoleId, command.Phone);
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    /// <summary>
    ///     Updates profile fields only — deliberately cannot touch the
    ///     password (see ChangePasswordCommand), so this can never silently
    ///     wipe it.
    /// </summary>
    public async Task<Result<User>> Handle(UpdateUserProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result<User>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.UpdateProfile(command.Name, command.LastName, command.Phone);
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<User>.Success(user);
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!(await changePasswordValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result.Failure(IamError.WeakPassword, localizer[nameof(IamError.WeakPassword)]);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        if (!hashingService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            return Result.Failure(IamError.CurrentPasswordInvalid, localizer[nameof(IamError.CurrentPasswordInvalid)]);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     Signing out used to be purely client-side (clear local storage,
    ///     nothing server-side ever ran) — a token leaked before this point
    ///     stayed valid until it naturally expired even after the user "logged
    ///     out". Bumping TokenVersion here makes the session that just ended
    ///     stop working immediately, everywhere, not just in this browser.
    /// </summary>
    public async Task<Result> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.RevokeAllSessions();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     Removing the business's only administrator is refused. Nothing
    ///     stopped it before, and it is unrecoverable: there is no password
    ///     reset and no support path, so an owner deleting their own account —
    ///     which the endpoint happily allowed — left the bodega's products,
    ///     sales and credit permanently unreachable, with the rows still in the
    ///     database and no one able to log in and administer them.
    /// </summary>
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        if (command.UserId == command.ActingUserId)
            return Result.Failure(IamError.CannotRemoveOwnAccess, localizer[nameof(IamError.CannotRemoveOwnAccess)]);

        if (await IsLastActiveAdminAsync(user, cancellationToken))
            return Result.Failure(IamError.CannotRemoveLastAdmin, localizer[nameof(IamError.CannotRemoveLastAdmin)]);

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    ///     The "can't remove the last admin" rule from DeleteUserCommand
    ///     applies just as much here: suspending a user takes away their
    ///     access exactly like deleting them, so a business could otherwise
    ///     be left with nobody able to sign in and administer it, with every
    ///     account still sitting in the database and no way back in.
    ///
    ///     The self-check matters even more here than on delete: deactivating
    ///     bumps TokenVersion, which kills the caller's own session the
    ///     instant it runs — an admin who suspends themselves has no token
    ///     left to call ReactivateUserCommand with, and (unlike delete) no
    ///     other admin needs to exist for this to happen, so it's reachable
    ///     any time a business has 2+ admins.
    /// </summary>
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        if (command.UserId == command.ActingUserId)
            return Result.Failure(IamError.CannotRemoveOwnAccess, localizer[nameof(IamError.CannotRemoveOwnAccess)]);

        if (await IsLastActiveAdminAsync(user, cancellationToken))
            return Result.Failure(IamError.CannotRemoveLastAdmin, localizer[nameof(IamError.CannotRemoveLastAdmin)]);

        user.Deactivate();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.Reactivate();
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>Shared by DeleteUserCommand and DeactivateUserCommand — both take away an admin's access permanently enough to need this guard.</summary>
    private async Task<bool> IsLastActiveAdminAsync(User user, CancellationToken cancellationToken)
    {
        if (user.RoleId != DefaultOwnerRoleId) return false;

        var team = await userRepository.FindAllByBusinessIdAsync(user.BusinessId, cancellationToken);
        var remainingAdmins = team.Count(member =>
            member.Id != user.Id && member.RoleId == DefaultOwnerRoleId && member.Status == UserStatus.Active);

        return remainingAdmins == 0;
    }

    /// <summary>Role has no BusinessId (fixed platform-wide catalog), so this lookup is never tenant-filtered.</summary>
    private async Task<string> ResolveRoleNameAsync(int roleId, CancellationToken cancellationToken)
    {
        var role = await roleRepository.FindByIdAsync(roleId, cancellationToken);
        return role?.Position ?? string.Empty;
    }
}
