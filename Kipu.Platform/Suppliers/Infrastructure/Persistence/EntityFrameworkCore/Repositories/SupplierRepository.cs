using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Repositories;

namespace Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class SupplierRepository(AppDbContext context) : BaseRepository<Supplier>(context), ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Supplier>().Where(supplier => supplier.BusinessId == businessId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<int>> FindExistingIdsAsync(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<Supplier>()
            .Where(supplier => supplier.BusinessId == businessId && supplierIds.Contains(supplier.Id))
            .Select(supplier => supplier.Id)
            .ToListAsync(cancellationToken);
    }
}
