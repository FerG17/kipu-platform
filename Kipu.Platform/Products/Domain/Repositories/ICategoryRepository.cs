using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<IEnumerable<Category>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(int businessId, string name, CancellationToken cancellationToken = default);
}
