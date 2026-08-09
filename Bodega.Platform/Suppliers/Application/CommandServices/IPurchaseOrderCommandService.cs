using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Commands;

namespace Bodega.Platform.Suppliers.Application.CommandServices;

public interface IPurchaseOrderCommandService
{
    Task<Result<PurchaseOrder>> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken);
    Task<Result<PurchaseOrder>> Handle(UpdatePurchaseOrderStatusCommand command, CancellationToken cancellationToken);
}
