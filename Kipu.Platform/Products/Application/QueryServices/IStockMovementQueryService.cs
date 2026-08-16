using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IStockMovementQueryService
{
    Task<IEnumerable<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query, CancellationToken cancellationToken);
}
