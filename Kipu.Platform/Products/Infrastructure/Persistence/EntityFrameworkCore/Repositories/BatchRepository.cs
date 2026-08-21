using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class BatchRepository(AppDbContext context) : BaseRepository<Batch>(context), IBatchRepository
{
    public async Task<IEnumerable<Batch>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>().Where(batch => batch.ProductId == productId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Batch>> FindActiveByInventoryItemIdAsync(int inventoryItemId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>()
            .Where(batch => EF.Property<int>(batch, "InventoryItemId") == inventoryItemId && batch.Status == BatchStatus.Active)
            // Nulls last: a batch with no expiration carries no urgency, so
            // FEFO should exhaust every dated lot before touching it.
            .OrderBy(batch => batch.Expiration == null)
            .ThenBy(batch => batch.Expiration)
            .ToListAsync(cancellationToken);
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
