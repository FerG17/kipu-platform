using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Products.Domain.Model.Queries;

public record GetAllStockMovementsByBusinessIdQuery(int BusinessId, PageRequest Page);
