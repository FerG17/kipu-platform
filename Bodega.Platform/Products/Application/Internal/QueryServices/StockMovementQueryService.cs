using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.QueryServices;

public class StockMovementQueryService(IStockMovementRepository stockMovementRepository) : IStockMovementQueryService
{
    public async Task<IEnumerable<StockMovement>> Handle(GetAllStockMovementsByBusinessIdQuery query,
        CancellationToken cancellationToken)
    {
        return await stockMovementRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }
}
