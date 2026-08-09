using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.QueryServices;

public class InventoryQueryService(IInventoryItemRepository inventoryItemRepository) : IInventoryQueryService
{
    public async Task<IEnumerable<InventoryItem>> Handle(GetInventoryByBusinessIdQuery query, CancellationToken cancellationToken)
    {
        return await inventoryItemRepository.FindAllByBusinessIdAsync(query.BusinessId, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> Handle(GetInventoryByProductIdQuery query, CancellationToken cancellationToken)
    {
        return await inventoryItemRepository.FindAllByProductIdAsync(query.ProductId, cancellationToken);
    }
}
