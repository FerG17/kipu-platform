namespace Bodega.Platform.Iam.Interfaces.Rest.Resources;

public record AuthenticatedUserResource(
    int Id,
    string Email,
    string Name,
    string LastName,
    int BusinessId,
    int RoleId,
    string Status,
    string Phone,
    string Token);
