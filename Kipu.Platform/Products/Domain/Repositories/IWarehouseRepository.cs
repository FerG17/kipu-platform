using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Domain.Repositories;

public interface IWarehouseRepository : IBaseRepository<Warehouse>
{
    Task<IEnumerable<Warehouse>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
