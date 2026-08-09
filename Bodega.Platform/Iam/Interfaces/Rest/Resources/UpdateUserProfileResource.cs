namespace Bodega.Platform.Iam.Interfaces.Rest.Resources;

public record UpdateUserProfileResource(string Name, string LastName, string Phone = "");
