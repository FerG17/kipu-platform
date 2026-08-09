using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;

namespace Bodega.Platform.Suppliers.Domain.Repositories;

public interface ISupplierRepository : IBaseRepository<Supplier>
{
    Task<IEnumerable<Supplier>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
