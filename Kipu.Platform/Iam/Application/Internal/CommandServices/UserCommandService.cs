using System.Data;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Iam.Domain.Model.Entities;
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
    IPasswordResetCodeRepository passwordResetCodeRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    IProductContextFacade productContextFacade,
    IValidator<SignUpCommand> signUpValidator,
    IValidator<InviteUserCommand> inviteUserValidator,
    IValidator<ChangePasswordCommand> changePasswordValidator,
    IValidator<ResetPasswordCommand> resetPasswordValidator,
    IValidator<UpdateUserProfileCommand> updateUserProfileValidator,
    IOptions<PasswordResetSettings> passwordResetSettings,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<UserCommandService> logger,
    IStringLocalizer<IamMessages> localizer,
    TimeProvider timeProvider)
    : IUserCommandService
{
    private const int DefaultOwnerRoleId = 1; // ADMIN — matches the frontend's hardcoded sign-up roleId.
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     A real BCrypt hash of a value nobody could ever type as a password
    ///     (it's not tied to any account) — computed once, not per request.
    ///     Verifying against this when the email doesn't exist keeps sign-in's
    ///     response time the same either way. Without it, `user == null`
    ///     short-circuits past VerifyPassword entirely, and a real account's
    ///     measurably slower BCrypt comparison becomes a timing oracle an
    ///     attacker can use to enumerate which emails are registered.
    /// </summary>
    private static readonly string DummyPasswordHash =
        global::BCrypt.Net.BCrypt.HashPassword("kipu-constant-time-decoy-9f3a2c7e-not-a-real-password");

    public async Task<Result<(User user, string token)>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        var passwordMatches = hashingService.VerifyPassword(command.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user == null || !passwordMatches)
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
        // Trim + lowercase so "Test@Example.com " and "test@example.com"
        // aren't treated as two different accounts by ExistsByEmailAsync.
        command = command with { Email = (command.Email ?? string.Empty).Trim().ToLowerInvariant() };

        var signUpValidation = await signUpValidator.ValidateAsync(command, cancellationToken);
        if (!signUpValidation.IsValid)
        {
            // Every failure used to report WeakPassword regardless of which
            // field actually failed — a too-long name told the owner their
            // password was the problem.
            var error = signUpValidation.Errors.Any(failure => failure.PropertyName == nameof(SignUpCommand.Password))
                ? IamError.WeakPassword
                : IamError.InvalidUserData;
            return Result<(User user, string token)>.Failure(error, localizer[error.ToString()]);
        }

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
        // Same normalization as SignUp — see its comment.
        command = command with { Email = (command.Email ?? string.Empty).Trim().ToLowerInvariant() };

        var inviteValidation = await inviteUserValidator.ValidateAsync(command, cancellationToken);
        if (!inviteValidation.IsValid)
        {
            var error = inviteValidation.Errors.Any(failure => failure.PropertyName == nameof(InviteUserCommand.Password))
                ? IamError.WeakPassword
                : IamError.InvalidUserData;
            return Result<User>.Failure(error, localizer[error.ToString()]);
        }

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
        if (!(await updateUserProfileValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<User>.Failure(IamError.InvalidUserData, localizer[nameof(IamError.InvalidUserData)]);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result<User>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.UpdateProfile(command.Name, command.LastName, command.Phone);
        userRepository.Update(user);
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<User>.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
        return Result<User>.Success(user);
    }

    /// <summary>
    ///     Returns a fresh token: UpdatePasswordHash bumps TokenVersion, which
    ///     RequestAuthorizationMiddleware checks on every request — without a
    ///     new token to replace the caller's own session cookie with, their
    ///     very next request would 401 them out of the account they just
    ///     changed the password for.
    /// </summary>
    public async Task<Result<string>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (!(await changePasswordValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<string>.Failure(IamError.WeakPassword, localizer[nameof(IamError.WeakPassword)]);

        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result<string>.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        if (!hashingService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            return Result<string>.Failure(IamError.CurrentPasswordInvalid, localizer[nameof(IamError.CurrentPasswordInvalid)]);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        userRepository.Update(user);
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<string>.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }

        var roleName = await ResolveRoleNameAsync(user.RoleId, cancellationToken);
        return Result<string>.Success(tokenService.GenerateToken(user, roleName));
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
        return await CompleteWithConcurrencyGuardAsync(cancellationToken);
    }

    /// <summary>
    ///     Removing the business's only administrator is refused. Nothing
    ///     stopped it before, and it is unrecoverable: there is no password
    ///     reset and no support path, so an owner deleting their own account —
    ///     which the endpoint happily allowed — left the bodega's products,
    ///     sales and credit permanently unreachable, with the rows still in the
    ///     database and no one able to log in and administer them.
    ///
    ///     Wrapped in a Serializable transaction (see IsLastActiveAdminAsync's
    ///     doc comment for why a plain read-then-write isn't enough): two
    ///     concurrent calls against the business's last two admins now
    ///     genuinely conflict at the database level instead of each reading
    ///     "at least one other admin survives" before either write commits.
    /// </summary>
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        await using var transaction =
            await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
            if (user == null)
                return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

            if (command.UserId == command.ActingUserId)
                return Result.Failure(IamError.CannotRemoveOwnAccess, localizer[nameof(IamError.CannotRemoveOwnAccess)]);

            if (await IsLastActiveAdminAsync(user, cancellationToken))
                return Result.Failure(IamError.CannotRemoveLastAdmin, localizer[nameof(IamError.CannotRemoveLastAdmin)]);

            userRepository.Remove(user);
            await unitOfWork.CompleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            // Covers both an EF concurrency-token mismatch and a raw MySQL
            // deadlock/lock-wait-timeout — Serializable isolation makes the
            // last-admin race in the doc comment above resolve as exactly
            // one of the two concurrent calls failing this way, never both
            // silently succeeding.
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
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
    ///
    ///     See Handle(DeleteUserCommand) for why this runs inside a
    ///     Serializable transaction.
    /// </summary>
    public async Task<Result> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        await using var transaction =
            await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
            if (user == null)
                return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

            if (command.UserId == command.ActingUserId)
                return Result.Failure(IamError.CannotRemoveOwnAccess, localizer[nameof(IamError.CannotRemoveOwnAccess)]);

            if (await IsLastActiveAdminAsync(user, cancellationToken))
                return Result.Failure(IamError.CannotRemoveLastAdmin, localizer[nameof(IamError.CannotRemoveLastAdmin)]);

            user.Deactivate();
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
    }

    public async Task<Result> Handle(ReactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        user.Reactivate();
        userRepository.Update(user);
        return await CompleteWithConcurrencyGuardAsync(cancellationToken);
    }

    /// <summary>
    ///     Always succeeds, whether or not the email belongs to a real,
    ///     active account — answering differently would let an attacker use
    ///     this endpoint to test which emails are registered. Same reasoning
    ///     applies to a request that arrives within the cooldown, or against
    ///     an account whose current code is already fully guessed: both
    ///     still 200, they just don't do anything new.
    ///
    ///     A code's AttemptCount carries over to its replacement instead of
    ///     resetting to 0 (see PasswordResetCode) unless the previous code
    ///     had already expired on its own — otherwise "5 wrong guesses" would
    ///     really mean "5 wrong guesses per resend", since nothing stopped
    ///     requesting a fresh code specifically to reset the counter.
    ///
    ///     The email itself is sent in the background, not awaited before
    ///     returning: awaiting it would make a real, active account's
    ///     response measurably slower than a nonexistent/inactive one's
    ///     (network round-trip to Resend vs. an instant no-op) — exactly the
    ///     kind of timing side-channel this endpoint's constant "always 200"
    ///     response is supposed to prevent. It runs in its own DI scope
    ///     (IServiceScopeFactory) since the request's own scope — and
    ///     whatever it holds, like the DbContext — is gone by the time a
    ///     background task actually runs.
    /// </summary>
    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        if (user == null || user.Status != UserStatus.Active) return Result.Success();

        var now = timeProvider.GetUtcNow();
        var existingCode = await passwordResetCodeRepository.FindLatestByUserIdAsync(user.Id, cancellationToken);

        if (existingCode != null)
        {
            var cooldown = TimeSpan.FromSeconds(passwordResetSettings.Value.RequestCooldownSeconds);
            if (existingCode.IsWithinCooldown(now, cooldown)) return Result.Success();

            if (!existingCode.IsExpired(now) && existingCode.HasExceededAttempts()) return Result.Success();
        }

        var carriedOverAttempts = existingCode != null && !existingCode.IsExpired(now) ? existingCode.AttemptCount : 0;
        var code = GenerateSixDigitCode();
        await passwordResetCodeRepository.RemoveAllForUserAsync(user.Id, cancellationToken);
        await passwordResetCodeRepository.AddAsync(
            new PasswordResetCode(user.Id, hashingService.HashPassword(code), now, now + ResetCodeLifetime,
                carriedOverAttempts),
            cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        SendPasswordResetEmailInBackground(user.Email, user.Name, code);
        return Result.Success();
    }

    /// <summary>
    ///     Fire-and-forget on purpose (see Handle(RequestPasswordResetCommand)
    ///     for why) — resolves IEmailService from a brand-new scope rather
    ///     than the captured one, since the request's scope (and its
    ///     DbContext) is disposed the moment the HTTP response finishes,
    ///     which can race with or outright precede this task actually running.
    /// </summary>
    private void SendPasswordResetEmailInBackground(string toEmail, string toName, string code)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await scopedEmailService.SendPasswordResetCodeAsync(toEmail, toName, code, CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send the background password-reset email to {Email}", toEmail);
            }
        });
    }

    /// <summary>
    ///     Checks the code without resetting anything yet — lets the UI move
    ///     to the "choose a new password" screen only once the code itself is
    ///     confirmed right, instead of finding out after typing a new
    ///     password too. Every wrong guess counts against the attempt limit,
    ///     matching ResetPasswordCommand's own re-check of the same code.
    ///
    ///     Status is re-checked here too (RequestPasswordResetCommand already
    ///     refuses to issue a code to a suspended account, but a still-valid
    ///     code from before a suspension shouldn't keep working either).
    /// </summary>
    public async Task<Result> Handle(VerifyPasswordResetCodeCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        if (user == null || user.Status != UserStatus.Active)
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        var resetCode = await passwordResetCodeRepository.FindLatestByUserIdAsync(user.Id, cancellationToken);
        if (resetCode == null || resetCode.IsExpired(timeProvider.GetUtcNow()))
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        // Already verified by an earlier call against this same code (e.g. a
        // double-click/double-submit retry) — re-checking the same correct
        // code should succeed again, not fail just because it was already
        // marked verified. Only a genuinely different/wrong code is rejected.
        if (resetCode.IsVerified)
        {
            return hashingService.VerifyPassword(command.Code, resetCode.CodeHash)
                ? Result.Success()
                : Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);
        }

        if (!resetCode.CanBeAttempted(timeProvider.GetUtcNow()))
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        try
        {
            if (!hashingService.VerifyPassword(command.Code, resetCode.CodeHash))
            {
                resetCode.RegisterFailedAttempt();
                passwordResetCodeRepository.Update(resetCode);
                await unitOfWork.CompleteAsync(cancellationToken);
                return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);
            }

            resetCode.MarkVerified();
            passwordResetCodeRepository.Update(resetCode);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two guesses against the same code landed at once. Reload and
            // check whether the request that won the race verified with this
            // same code — if so this was just a race, not a real conflict, so
            // succeed too instead of making the user re-enter a code that was
            // actually correct.
            var reloaded = await passwordResetCodeRepository.FindLatestByUserIdAsync(user.Id, cancellationToken);
            if (reloaded is { IsVerified: true } && hashingService.VerifyPassword(command.Code, reloaded.CodeHash))
                return Result.Success();

            return Result.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
    }

    /// <summary>
    ///     Only succeeds against a code already verified by
    ///     VerifyPasswordResetCodeCommand — this deliberately re-checks the
    ///     code (not just the IsVerified flag) so a code that expired in the
    ///     gap between the two calls is still rejected. Bumps TokenVersion via
    ///     UpdatePasswordHash, so every session this user had open anywhere —
    ///     including whatever leaked/guessed credential led to needing a reset
    ///     in the first place — stops working immediately. Status is
    ///     re-checked too, same reasoning as VerifyPasswordResetCodeCommand.
    /// </summary>
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!(await resetPasswordValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result.Failure(IamError.WeakPassword, localizer[nameof(IamError.WeakPassword)]);

        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        if (user == null || user.Status != UserStatus.Active)
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        var resetCode = await passwordResetCodeRepository.FindLatestByUserIdAsync(user.Id, cancellationToken);
        if (resetCode == null || !resetCode.IsVerified || resetCode.IsExpired(timeProvider.GetUtcNow()) ||
            !hashingService.VerifyPassword(command.Code, resetCode.CodeHash))
            return Result.Failure(IamError.InvalidResetCode, localizer[nameof(IamError.InvalidResetCode)]);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        userRepository.Update(user);
        passwordResetCodeRepository.Remove(resetCode);
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two concurrent resets against the same verified code — without
            // PasswordResetCode's own concurrency token this could otherwise
            // let both succeed (Remove only marks for deletion; the actual
            // DELETE only happens here, at CompleteAsync).
            return Result.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
        return Result.Success();
    }

    /// <summary>Cryptographically random, not Random — this is a secret that grants account access, same bar as the password itself.</summary>
    private static string GenerateSixDigitCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private async Task<Result> CompleteWithConcurrencyGuardAsync(CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(IamError.ConcurrentModification, localizer[nameof(IamError.ConcurrentModification)]);
        }
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
