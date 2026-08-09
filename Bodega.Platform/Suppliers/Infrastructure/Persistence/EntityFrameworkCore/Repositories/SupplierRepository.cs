using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Repositories;

namespace Bodega.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SupplierRepository(AppDbContext context) : BaseRepository<Supplier>(context), ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Supplier>().Where(supplier => supplier.BusinessId == businessId).ToListAsync(cancellationToken);
    }
}
