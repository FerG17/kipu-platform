using Microsoft.Extensions.Localization;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Application.Internal.OutboundServices;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Domain.Model.Errors;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Iam.Resources;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Iam.Application.Internal.CommandServices;

/// <summary>
///     Handles every User-related command, including the atomic sign-up flow
///     (see Handle(SignUpCommand)).
/// </summary>
public class UserCommandService(
    IUserRepository userRepository,
    IBusinessRepository businessRepository,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork,
    IProductContextFacade productContextFacade,
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

        var token = tokenService.GenerateToken(user);
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

            var token = tokenService.GenerateToken(user);
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
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        if (!hashingService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            return Result.Failure(IamError.CurrentPasswordInvalid, localizer[nameof(IamError.CurrentPasswordInvalid)]);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword));
        userRepository.Update(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null) return Result.Failure(IamError.UserNotFound, localizer[nameof(IamError.UserNotFound)]);

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }
}
