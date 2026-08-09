using Bodega.Platform.Sales.Domain.Model.Aggregates;
using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Sales.Application.CommandServices;

public interface ISaleCommandService
{
    Task<Result<Sale>> Handle(CreateSaleCommand command, CancellationToken cancellationToken);
    Task<Result<Sale>> Handle(UpdateSaleStatusCommand command, CancellationToken cancellationToken);
}
