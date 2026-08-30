using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Interfaces.Rest.Resources;

namespace Kipu.Platform.Products.Interfaces.Rest.Transform;

public static class CategoryResourceFromEntityAssembler
{
    public static CategoryResource ToResourceFromEntity(Category category)
    {
        return new CategoryResource(category.Id, category.Name);
    }
}
