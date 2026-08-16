using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Sales.Application.CommandServices;

public interface ISaleCommandService
{
    Task<Result<Sale>> Handle(CreateSaleCommand command, CancellationToken cancellationToken);
    Task<Result<Sale>> Handle(UpdateSaleStatusCommand command, CancellationToken cancellationToken);
}
