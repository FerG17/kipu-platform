namespace Kipu.Platform.Iam.Interfaces.Rest.Resources;

public record VerifyResetCodeResource(string Email, string Code);
