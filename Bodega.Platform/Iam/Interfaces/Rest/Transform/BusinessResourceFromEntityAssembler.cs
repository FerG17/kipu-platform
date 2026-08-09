using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;

namespace Bodega.Platform.Iam.Interfaces.Rest.Transform;

public static class BusinessResourceFromEntityAssembler
{
    public static BusinessResource ToResourceFromEntity(Business business)
    {
        return new BusinessResource(business.Id, business.Name, business.Type, business.Address, business.Ruc,
            business.UserId);
    }
}
