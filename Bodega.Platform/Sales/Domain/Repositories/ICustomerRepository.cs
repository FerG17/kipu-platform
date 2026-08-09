using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Sales.Domain.Repositories;

public interface ICustomerRepository : IBaseRepository<Customer>
{
    Task<IEnumerable<Customer>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
