using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Products.Application.CommandServices;

public interface IWarehouseCommandService
{
    Task<Result<Warehouse>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken);
    Task<Result<Warehouse>> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken);
}
