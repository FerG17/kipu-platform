using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class CreateCustomerCommandFromResourceAssembler
{
    public static CreateCustomerCommand ToCommandFromResource(CreateCustomerResource resource, int businessId)
    {
        return new CreateCustomerCommand(businessId, resource.FullName, resource.DocumentNumber ?? string.Empty,
            resource.PhoneNumber ?? string.Empty, resource.Email ?? string.Empty);
    }
}
