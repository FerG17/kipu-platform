using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Sales.Domain.Repositories;

public interface ICustomerRepository : IBaseRepository<Customer>
{
    Task<IEnumerable<Customer>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
