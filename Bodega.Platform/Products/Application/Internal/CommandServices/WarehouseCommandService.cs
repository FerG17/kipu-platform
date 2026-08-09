using Microsoft.Extensions.Localization;
using Bodega.Platform.Products.Application.CommandServices;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Domain.Model.Errors;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Products.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Products.Application.Internal.CommandServices;

public class WarehouseCommandService(
    IWarehouseRepository warehouseRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<ProductMessages> localizer)
    : IWarehouseCommandService
{
    public async Task<Result<Warehouse>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        var warehouse = new Warehouse(command.BusinessId, command.Name, command.Code, command.Address, command.Capacity);
        await warehouseRepository.AddAsync(warehouse, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Warehouse>.Success(warehouse);
    }

    public async Task<Result<Warehouse>> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.FindByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse == null)
            return Result<Warehouse>.Failure(ProductError.WarehouseNotFound, localizer[nameof(ProductError.WarehouseNotFound)]);

        warehouse.UpdateDetails(command.Name, command.Code, command.Address, command.Capacity);
        if (command.Active) warehouse.Activate();
        else warehouse.Deactivate();

        warehouseRepository.Update(warehouse);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Warehouse>.Success(warehouse);
    }
}
