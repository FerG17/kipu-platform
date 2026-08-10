using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class BatchRepository(AppDbContext context) : BaseRepository<Batch>(context), IBatchRepository
{
    public async Task<IEnumerable<Batch>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>().Where(batch => batch.ProductId == productId).ToListAsync(cancellationToken);
    }

    public async Task<Batch?> FindActiveByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>()
            .FirstOrDefaultAsync(batch => batch.ProductId == productId && batch.Status == BatchStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<Batch>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>().Where(batch => batch.BusinessId == businessId).ToListAsync(cancellationToken);
    }

    /// <summary>IgnoreQueryFilters() deliberately — the alerts expiration sweep runs outside any authenticated business and needs every business's active batches, see IBatchRepository.</summary>
    public async Task<IEnumerable<Batch>> FindAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>().IgnoreQueryFilters()
            .Where(batch => batch.Status == BatchStatus.Active).ToListAsync(cancellationToken);
    }
}
