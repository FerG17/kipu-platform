using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Errors;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Products.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Products.Application.Internal.CommandServices;

public class WarehouseCommandService(
    IWarehouseRepository warehouseRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateWarehouseCommand> createWarehouseValidator,
    IValidator<UpdateWarehouseCommand> updateWarehouseValidator,
    IStringLocalizer<ProductMessages> localizer)
    : IWarehouseCommandService
{
    public async Task<Result<Warehouse>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        if (!(await createWarehouseValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Warehouse>.Failure(ProductError.InvalidWarehouseData,
                localizer[nameof(ProductError.InvalidWarehouseData)]);

        var warehouse = new Warehouse(command.BusinessId, command.Name, command.Code, command.Address, command.Capacity);
        await warehouseRepository.AddAsync(warehouse, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Warehouse>.Success(warehouse);
    }

    public async Task<Result<Warehouse>> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken)
    {
        if (!(await updateWarehouseValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Warehouse>.Failure(ProductError.InvalidWarehouseData,
                localizer[nameof(ProductError.InvalidWarehouseData)]);

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
