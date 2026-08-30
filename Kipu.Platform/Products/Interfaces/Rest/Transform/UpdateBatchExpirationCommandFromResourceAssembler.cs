using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class UpdateBatchExpirationCommandFromResourceAssembler
{
    public static UpdateBatchExpirationCommand ToCommandFromResource(UpdateBatchExpirationResource resource, int batchId)
    {
        return new UpdateBatchExpirationCommand(batchId, resource.Expiration, resource.Label);
    }
}
