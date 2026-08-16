using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Commands;

namespace Kipu.Platform.Suppliers.Application.CommandServices;

public interface IPurchaseOrderCommandService
{
    Task<Result<PurchaseOrder>> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken);
    Task<Result<PurchaseOrder>> Handle(UpdatePurchaseOrderStatusCommand command, CancellationToken cancellationToken);
}
