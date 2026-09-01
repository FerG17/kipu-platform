using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Shared.Domain.Repositories;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;

namespace Kipu.Platform.Suppliers.Domain.Repositories;

public interface ISupplierRepository : IBaseRepository<Supplier>
{
    Task<PagedResult<Supplier>> FindAllByBusinessIdAsync(int businessId, PageRequest page, bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>Narrows a candidate id set down to the ones that actually exist and belong to this business.</summary>
    Task<IReadOnlyCollection<int>> FindExistingIdsAsync(int businessId, IReadOnlyCollection<int> supplierIds,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk lookup by id, ignoring the tenant filter — feeds the supplier-installment-due alerts sweep with each order's supplier name.</summary>
    Task<IEnumerable<Supplier>> FindAllIgnoringTenantByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
