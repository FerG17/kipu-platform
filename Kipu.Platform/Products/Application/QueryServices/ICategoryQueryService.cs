using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Queries;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface ICategoryQueryService
{
    Task<IEnumerable<Category>> Handle(GetAllCategoriesByBusinessIdQuery query, CancellationToken cancellationToken);
}
