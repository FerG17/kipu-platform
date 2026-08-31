using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class UpdateProductCommandFromResourceAssembler
{
    public static UpdateProductCommand ToCommandFromResource(UpdateProductResource resource, int productId)
    {
        return new UpdateProductCommand(productId, resource.Name, resource.Description, resource.Category, resource.BasePrice,
            resource.Barcode, resource.SupplierIds, resource.UnitOfSale, resource.UnidadDeMedida, resource.Presentacion);
    }
}
