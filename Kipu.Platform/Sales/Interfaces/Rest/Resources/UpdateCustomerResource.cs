namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

/// <summary>Same nullability rationale as CreateCustomerResource (X5 #6).</summary>
public record UpdateCustomerResource(string FullName, string? DocumentNumber, string? PhoneNumber, string? Email);
