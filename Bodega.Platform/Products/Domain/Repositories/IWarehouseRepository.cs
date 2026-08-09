using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Domain.Repositories;

public interface IWarehouseRepository : IBaseRepository<Warehouse>
{
    Task<IEnumerable<Warehouse>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
}
