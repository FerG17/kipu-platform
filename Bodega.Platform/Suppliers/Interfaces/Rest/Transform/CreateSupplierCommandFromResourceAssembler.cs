using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Bodega.Platform.Suppliers.Interfaces.Rest.Transform;

public static class CreateSupplierCommandFromResourceAssembler
{
    public static CreateSupplierCommand ToCommandFromResource(CreateSupplierResource resource, int businessId)
    {
        return new CreateSupplierCommand(businessId, resource.Name, resource.LastName, resource.Ruc, resource.Email,
            resource.Phone, resource.Address, resource.ContactPerson, resource.Category);
    }
}
