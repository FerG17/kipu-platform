using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Interfaces.Rest.Resources;

namespace Bodega.Platform.Products.Interfaces.Rest.Transform;

public static class UpdateMinimumStockCommandFromResourceAssembler
{
    public static UpdateMinimumStockCommand ToCommandFromResource(UpdateMinimumStockResource resource, int productId)
    {
        return new UpdateMinimumStockCommand(productId, resource.MinimumStock);
    }
}
