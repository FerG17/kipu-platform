namespace Bodega.Platform.Iam.Domain.Model.Errors;

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
    OperationCancelled,
    DatabaseError,
    InternalServerError
}
