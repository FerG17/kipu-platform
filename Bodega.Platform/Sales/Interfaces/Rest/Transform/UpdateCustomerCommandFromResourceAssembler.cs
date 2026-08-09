using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class UpdateCustomerCommandFromResourceAssembler
{
    public static UpdateCustomerCommand ToCommandFromResource(UpdateCustomerResource resource, int customerId)
    {
        return new UpdateCustomerCommand(customerId, resource.FullName, resource.DocumentNumber, resource.PhoneNumber,
            resource.Email);
    }
}
