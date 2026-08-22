using Kipu.Platform.Shared.Domain.Model.ValueObjects;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Queries;

namespace Kipu.Platform.Suppliers.Application.QueryServices;

public interface IPurchaseOrderQueryService
{
    Task<PagedResult<PurchaseOrder>> Handle(GetAllPurchaseOrdersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PurchaseOrder>> Handle(GetPurchaseOrdersBySupplierIdQuery query, CancellationToken cancellationToken);
    Task<PurchaseOrder?> Handle(GetPurchaseOrderByIdQuery query, CancellationToken cancellationToken);
}
