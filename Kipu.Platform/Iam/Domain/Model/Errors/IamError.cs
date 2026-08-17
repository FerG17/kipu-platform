namespace Kipu.Platform.Iam.Domain.Model.Errors;

public enum IamError
{
    InvalidCredentials,
    EmailAlreadyTaken,
    UserNotFound,
    BusinessNotFound,
    CurrentPasswordInvalid,
    WeakPassword,
    RoleNotFound,

    /// <summary>Removing this user would leave the business with nobody who can administer it, and there is no recovery path.</summary>
    CannotRemoveLastAdmin,

    /// <summary>
    ///     Deleting or suspending your own account has no way back — deleting
    ///     is unrecoverable, and suspending revokes the very session you'd
    ///     need to undo it with. Another admin has to do it instead.
    /// </summary>
    CannotRemoveOwnAccess,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}
