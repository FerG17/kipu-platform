using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Products.Application.CommandServices;

public interface IWarehouseCommandService
{
    Task<Result<Warehouse>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken);
    Task<Result<Warehouse>> Handle(UpdateWarehouseCommand command, CancellationToken cancellationToken);
}
