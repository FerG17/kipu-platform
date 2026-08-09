namespace Bodega.Platform.Iam.Domain.Model.Commands;

/// <summary>
///     Creates a User and its Business atomically (one transaction, one
///     SaveChanges) — see UserCommandService.Handle(SignUpCommand). Fixes the
///     historical bug where sign-up left businessId permanently null.
/// </summary>
public record SignUpCommand(
    string Email,
    string Password,
    string Name,
    string LastName,
    string Phone,
    string BusinessName,
    string BusinessType,
    string Ruc,
    string Address);
