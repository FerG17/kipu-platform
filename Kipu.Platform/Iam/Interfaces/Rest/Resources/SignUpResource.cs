namespace Kipu.Platform.Iam.Interfaces.Rest.Resources;

public record SignUpResource(
    string Email,
    string Password,
    string Name,
    string LastName,
    string BusinessName,
    string BusinessType,
    string Phone = "",
    string Ruc = "",
    string Address = "");
