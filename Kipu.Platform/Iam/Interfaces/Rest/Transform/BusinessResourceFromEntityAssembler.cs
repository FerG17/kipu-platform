using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;

namespace Kipu.Platform.Iam.Interfaces.Rest.Transform;

public static class BusinessResourceFromEntityAssembler
{
    public static BusinessResource ToResourceFromEntity(Business business)
    {
        return new BusinessResource(business.Id, business.Name, business.Type, business.Address, business.Ruc,
            business.UserId);
    }
}
