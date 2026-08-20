using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Shared.Domain.Model.ValueObjects;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

public class StockMovementQueryService(IStockMovementRepository stockMovementRepository) : IStockMovementQueryService
{
    public async Task<PagedResult<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await stockMovementRepository.FindAllByBusinessIdAsync(query.BusinessId, query.Page, cancellationToken);
    }
}
