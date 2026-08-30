using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

public class CategoryQueryService(ICategoryRepository categoryRepository) : ICategoryQueryService
{
    public async Task<IEnumerable<Category>> Handle(GetAllCategoriesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await categoryRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
