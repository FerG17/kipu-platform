using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

public class StockMovementQueryService(IStockMovementRepository stockMovementRepository) : IStockMovementQueryService
{
    public async Task<IEnumerable<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await stockMovementRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
