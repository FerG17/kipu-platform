using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class BatchRepository(AppDbContext context) : BaseRepository<Batch>(context), IBatchRepository
{
    /// <summary>
    ///     Overrides the base lookup to eager-load InventoryItem — without it,
    ///     BatchResource's InventoryId (Batch.InventoryItem?.Id) always came
    ///     back null for anything read outside the SaveChanges call that
    ///     created the batch, since EF Core never lazy-loads it on its own.
    ///     Callers here (DiscardBatchCommand, UpdateBatchExpirationCommand)
    ///     both return their Batch straight into a BatchResource.
    /// </summary>
    public override async Task<Batch?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>()
            .Include(batch => batch.InventoryItem)
            .FirstOrDefaultAsync(batch => batch.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Batch>> FindAllByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>()
            .Include(batch => batch.InventoryItem)
            .Where(batch => batch.ProductId == productId).ToListAsync(cancellationToken);
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
        return await Context.Set<Batch>()
            .Include(batch => batch.InventoryItem)
            .Where(batch => batch.BusinessId == businessId).ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     IgnoreQueryFilters() deliberately — the alerts expiration sweep runs outside any authenticated business
    ///     and needs every business's active batches, see IBatchRepository. RemainingQuantity > 0 excludes batches a
    ///     sale has emptied down to zero: those should already be Discard()ed (see
    ///     InventoryCommandService.Handle(RegisterStockSaleCommand)), but this filter keeps the sweep correct even
    ///     if some other path ever zeroes a batch out without discarding it (X6 #13).
    /// </summary>
    public async Task<IEnumerable<Batch>> FindAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Batch>().IgnoreQueryFilters()
            .Where(batch => batch.Status == BatchStatus.Active && batch.RemainingQuantity > 0).ToListAsync(cancellationToken);
    }
}
