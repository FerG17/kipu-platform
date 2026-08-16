using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class CustomerResourceFromEntityAssembler
{
    public static CustomerResource ToResourceFromEntity(Customer customer)
    {
        return new CustomerResource(customer.Id, customer.BusinessId, customer.FullName, customer.DocumentNumber,
            customer.PhoneNumber, customer.Email, customer.RegisteredAt);
    }
}
