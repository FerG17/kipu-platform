using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class CustomerResourceFromEntityAssembler
{
    public static CustomerResource ToResourceFromEntity(Customer customer)
    {
        return new CustomerResource(customer.Id, customer.BusinessId, customer.FullName, customer.DocumentNumber,
            customer.PhoneNumber, customer.Email, customer.RegisteredAt);
    }
}
