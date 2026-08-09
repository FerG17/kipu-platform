namespace Bodega.Platform.Sales.Domain.Model.Commands;

public record CreateCustomerCommand(int BusinessId, string FullName, string DocumentNumber, string PhoneNumber, string Email);
