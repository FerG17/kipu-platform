using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IStockMovementQueryService
{
    Task<PagedResult<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query, CancellationToken cancellationToken);

    Task<IEnumerable<StockMovement>> Handle(GetFilteredStockMovementsQuery query, CancellationToken cancellationToken);
}
