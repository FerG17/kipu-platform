using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Entities;

namespace Bodega.Platform.Suppliers.Domain.Repositories;

public interface IPurchaseOrderRepository : IBaseRepository<PurchaseOrder>
{
    Task<IEnumerable<PurchaseOrder>> FindAllByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseOrder>> FindAllBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);

    /// <summary>FindByIdAsync (from IBaseRepository) does not eager-load Details — use this when the lines are needed.</summary>
    Task<PurchaseOrder?> FindByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Locates the order that owns a given detail line — used by ISupplierContextFacade for Delivery Tracking's autofill.</summary>
    Task<(PurchaseOrder Order, PurchaseOrderDetail Detail)?> FindByDetailIdAsync(int detailId, CancellationToken cancellationToken = default);
}
