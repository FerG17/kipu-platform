namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record CreateCustomerResource(string FullName, string DocumentNumber, string PhoneNumber, string Email);
