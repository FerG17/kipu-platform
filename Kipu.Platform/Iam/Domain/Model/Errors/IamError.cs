namespace Kipu.Platform.Iam.Domain.Model.Errors;

public enum IamError
{
    InvalidCredentials,
    EmailAlreadyTaken,
    UserNotFound,
    BusinessNotFound,
    CurrentPasswordInvalid,
    WeakPassword,

    /// <summary>Name/LastName/Phone/Email outside what their columns accept, or a malformed email.</summary>
    InvalidUserData,

    /// <summary>Name/Type/Address/Ruc outside what their columns accept, or Type outside the known set.</summary>
    InvalidBusinessData,

    RoleNotFound,

    /// <summary>Removing this user would leave the business with nobody who can administer it, and there is no recovery path.</summary>
    CannotRemoveLastAdmin,

    /// <summary>
    ///     Deleting or suspending your own account has no way back — deleting
    ///     is unrecoverable, and suspending revokes the very session you'd
    ///     need to undo it with. Another admin has to do it instead.
    /// </summary>
    CannotRemoveOwnAccess,

    /// <summary>
    ///     The submitted 6-digit reset code is wrong, expired, already used,
    ///     or has been guessed too many times — deliberately one generic
    ///     error for all of those, so a wrong guess can't be distinguished
    ///     from "you waited too long" or "someone already used it".
    /// </summary>
    InvalidResetCode,

    /// <summary>
    ///     Another request modified this same row (User or PasswordResetCode)
    ///     in the gap between this request's read and its write — e.g. two
    ///     concurrent attempts to deactivate the business's last two admins,
    ///     or two concurrent resets against the same verified code. Whoever
    ///     loses the race should retry with fresh data rather than the write
    ///     silently overwriting a change it never saw.
    /// </summary>
    ConcurrentModification,

    OperationCancelled,
    DatabaseError,
    InternalServerError
}
