using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class ProductResourceFromEntityAssembler
{
    public static ProductResource ToResourceFromEntity(Product product, IReadOnlyCollection<int> supplierIds)
    {
        return new ProductResource(product.Id, product.BusinessId, product.Name, product.Description,
            product.Category, product.BasePrice, product.Status, product.Barcode, supplierIds);
    }
}
