using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Queries;

namespace Bodega.Platform.Suppliers.Application.QueryServices;

public interface IPurchaseOrderQueryService
{
    Task<IEnumerable<PurchaseOrder>> Handle(GetAllPurchaseOrdersByBusinessIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<PurchaseOrder>> Handle(GetPurchaseOrdersBySupplierIdQuery query, CancellationToken cancellationToken);
    Task<PurchaseOrder?> Handle(GetPurchaseOrderByIdQuery query, CancellationToken cancellationToken);
}
