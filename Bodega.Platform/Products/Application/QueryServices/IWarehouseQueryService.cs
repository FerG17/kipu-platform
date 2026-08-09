using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Queries;

namespace Bodega.Platform.Products.Application.QueryServices;

public interface IWarehouseQueryService
{
    Task<IEnumerable<Warehouse>> Handle(GetAllWarehousesByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Warehouse?> Handle(GetWarehouseByIdQuery query, CancellationToken cancellationToken);
}
