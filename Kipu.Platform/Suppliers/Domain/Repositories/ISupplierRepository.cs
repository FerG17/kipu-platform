using Kipu.Platform.Shared.Domain.Repositories;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;

namespace Kipu.Platform.Suppliers.Domain.Repositories;

public interface ISupplierRepository : IBaseRepository<Supplier>
{
    Task<IEnumerable<Supplier>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

    /// <summary>Narrows a candidate id set down to the ones that actually exist and belong to this business.</summary>
    Task<IReadOnlyCollection<int>> FindExistingIdsAsync(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken = default);
}
