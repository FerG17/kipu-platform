using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Suppliers.Domain.Model.Queries;

public record GetAllPurchaseOrdersByBusinessIdQuery(int BusinessId, PageRequest Page);
