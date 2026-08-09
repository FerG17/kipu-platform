namespace Bodega.Platform.Sales.Interfaces.Rest.Resources;

public record UpdateCustomerResource(string FullName, string DocumentNumber, string PhoneNumber, string Email);
