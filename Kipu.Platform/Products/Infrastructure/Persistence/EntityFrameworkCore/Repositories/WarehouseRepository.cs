using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class WarehouseRepository(AppDbContext context) : BaseRepository<Warehouse>(context), IWarehouseRepository
{
    public async Task<IEnumerable<Warehouse>> FindAllByBusinessIdAsync(int businessId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Warehouse>().Where(warehouse => warehouse.BusinessId == businessId)
            .ToListAsync(cancellationToken);
    }
}
