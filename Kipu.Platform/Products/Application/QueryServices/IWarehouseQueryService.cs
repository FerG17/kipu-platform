using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Queries;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IWarehouseQueryService
{
    Task<IEnumerable<Warehouse>> Handle(GetAllWarehousesByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<Warehouse?> Handle(GetWarehouseByIdQuery query, CancellationToken cancellationToken);
}
