using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Products.Application.CommandServices;

public interface IInventoryCommandService
{
    Task<Result<InventoryItem>> Handle(RegisterStockIntakeCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(RegisterStockSaleCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(RegisterStockReturnCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(UpdateMinimumStockCommand command, CancellationToken cancellationToken);
    Task<Result<InventoryItem>> Handle(AdjustStockCommand command, CancellationToken cancellationToken);
    Task<Result<Batch>> Handle(DiscardBatchCommand command, CancellationToken cancellationToken);
}
