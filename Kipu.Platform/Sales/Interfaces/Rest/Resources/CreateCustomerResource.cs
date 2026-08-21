namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

/// <summary>
///     DocumentNumber/PhoneNumber/Email are nullable: ASP.NET Core's
///     [ApiController] model binding otherwise infers [Required] from a
///     non-nullable reference-type record property, rejecting a request
///     that simply omits them with a 400 before it ever reaches
///     CreateCustomerCommandValidator — which only requires FullName (X5 #6).
/// </summary>
public record CreateCustomerResource(string FullName, string? DocumentNumber, string? PhoneNumber, string? Email);
