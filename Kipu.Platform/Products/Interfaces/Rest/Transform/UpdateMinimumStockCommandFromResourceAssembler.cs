using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class UpdateMinimumStockCommandFromResourceAssembler
{
    public static UpdateMinimumStockCommand ToCommandFromResource(UpdateMinimumStockResource resource, int productId)
    {
        return new UpdateMinimumStockCommand(productId, resource.MinimumStock);
    }
}
