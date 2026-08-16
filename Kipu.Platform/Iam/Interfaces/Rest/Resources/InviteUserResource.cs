namespace Kipu.Platform.Iam.Interfaces.Rest.Resources;

/// <summary>BusinessId is deliberately not part of this resource — it's always the current authenticated user's business.</summary>
public record InviteUserResource(string Email, string Password, string Name, string LastName, int RoleId, string Phone = "");
