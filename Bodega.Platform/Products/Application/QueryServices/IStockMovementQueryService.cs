using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;

namespace Bodega.Platform.Products.Application.QueryServices;

public interface IStockMovementQueryService
{
    Task<IEnumerable<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query, CancellationToken cancellationToken);
}
