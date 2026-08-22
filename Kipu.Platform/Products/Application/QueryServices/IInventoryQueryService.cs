using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;

namespace Kipu.Platform.Products.Application.QueryServices;

public interface IInventoryQueryService
{
    Task<IEnumerable<InventoryItem>> Handle(GetInventoryByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<InventoryItem>> Handle(GetInventoryByProductIdQuery query, CancellationToken cancellationToken);
}
