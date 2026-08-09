namespace Bodega.Platform.Iam.Domain.Model.Errors;

public enum IamError
{
    InvalidCredentials,
    EmailAlreadyTaken,
    UserNotFound,
    BusinessNotFound,
    CurrentPasswordInvalid,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}
