namespace Bodega.Platform.Iam.Interfaces.Rest.Resources;

public record ChangePasswordResource(string CurrentPassword, string NewPassword);
