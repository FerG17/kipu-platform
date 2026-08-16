using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class ProductSupplierRepository(AppDbContext context)
    : BaseRepository<ProductSupplier>(context), IProductSupplierRepository
{
    public async Task<IReadOnlyCollection<ProductSupplier>> FindByProductIdAsync(int productId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ProductSupplier>()
            .Where(link => link.ProductId == productId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyCollection<int>>> FindSupplierIdsGroupedByProductAsync(
        int businessId, CancellationToken cancellationToken = default)
    {
        var links = await Context.Set<ProductSupplier>()
            .Where(link => link.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(link => link.ProductId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<int>)group.Select(link => link.SupplierId).ToList());
    }
}
