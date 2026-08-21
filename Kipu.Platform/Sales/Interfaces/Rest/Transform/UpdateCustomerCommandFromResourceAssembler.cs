using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class UpdateCustomerCommandFromResourceAssembler
{
    public static UpdateCustomerCommand ToCommandFromResource(UpdateCustomerResource resource, int customerId)
    {
        return new UpdateCustomerCommand(customerId, resource.FullName, resource.DocumentNumber ?? string.Empty,
            resource.PhoneNumber ?? string.Empty, resource.Email ?? string.Empty);
    }
}
