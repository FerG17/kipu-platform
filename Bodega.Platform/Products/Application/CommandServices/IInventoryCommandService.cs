using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Products.Application.CommandServices;

public interface IInventoryCommandService
{
    Task<Result<InventoryItem>> Handle(RegisterStockIntakeCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(RegisterStockSaleCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(UpdateMinimumStockCommand command, CancellationToken cancellationToken);
    Task<Result<Batch>> Handle(CreateOrUpdateBatchCommand command, CancellationToken cancellationToken);
    Task<Result<Batch>> Handle(DiscardBatchCommand command, CancellationToken cancellationToken);
}
