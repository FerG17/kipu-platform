using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.QueryServices;

public class WarehouseQueryService(IWarehouseRepository warehouseRepository) : IWarehouseQueryService
{
    public async Task<IEnumerable<Warehouse>> Handle(GetAllWarehousesByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await warehouseRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<Warehouse?> Handle(GetWarehouseByIdQuery query, CancellationToken cancellationToken)
    {
        return await warehouseRepository.FindByIdAsync(query.WarehouseId, cancellationToken);
    }
}
