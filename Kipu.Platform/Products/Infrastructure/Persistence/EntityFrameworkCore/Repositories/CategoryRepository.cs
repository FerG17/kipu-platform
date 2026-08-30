using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CategoryRepository(AppDbContext context) : BaseRepository<Category>(context), ICategoryRepository
{
    public async Task<IEnumerable<Category>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Category>().Where(category => category.BusinessId == businessId)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int businessId, string name, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Category>()
            .AnyAsync(category => category.BusinessId == businessId && category.Name == name, cancellationToken);
    }
}
