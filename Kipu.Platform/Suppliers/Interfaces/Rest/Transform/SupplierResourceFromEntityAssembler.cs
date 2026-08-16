using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Kipu.Platform.Suppliers.Interfaces.Rest.Transform;

public static class SupplierResourceFromEntityAssembler
{
    public static SupplierResource ToResourceFromEntity(Supplier supplier)
    {
        return new SupplierResource(supplier.Id, supplier.BusinessId, supplier.Name, supplier.LastName, supplier.Ruc,
            supplier.Email, supplier.Phone, supplier.Address, supplier.ContactPerson, supplier.Category, supplier.Status,
            supplier.Since);
    }
}
