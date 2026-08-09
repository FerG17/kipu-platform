using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;

namespace Bodega.Platform.Sales.Interfaces.Rest.Transform;

public static class CreateCustomerCommandFromResourceAssembler
{
    public static CreateCustomerCommand ToCommandFromResource(CreateCustomerResource resource, int businessId)
    {
        return new CreateCustomerCommand(businessId, resource.FullName, resource.DocumentNumber, resource.PhoneNumber,
            resource.Email);
    }
}
