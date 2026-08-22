using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class CreateProductCommandFromResourceAssembler
{
    public static CreateProductCommand ToCommandFromResource(CreateProductResource resource, int businessId)
    {
        return new CreateProductCommand(businessId, resource.Name, resource.Description, resource.Category, resource.BasePrice,
            resource.Barcode, resource.SupplierIds);
    }
}
