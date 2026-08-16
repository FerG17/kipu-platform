using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.QueryServices;

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
