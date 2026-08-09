using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Bodega.Platform.Suppliers.Interfaces.Rest.Transform;

public static class UpdateSupplierCommandFromResourceAssembler
{
    public static UpdateSupplierCommand ToCommandFromResource(UpdateSupplierResource resource, int supplierId)
    {
        return new UpdateSupplierCommand(supplierId, resource.Name, resource.LastName, resource.Ruc, resource.Email,
            resource.Phone, resource.Address, resource.ContactPerson, resource.Category);
    }
}
