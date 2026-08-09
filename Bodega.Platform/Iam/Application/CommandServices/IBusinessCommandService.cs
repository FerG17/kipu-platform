using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Iam.Application.CommandServices;

public interface IBusinessCommandService
{
    Task<Result<Business>> Handle(UpdateBusinessCommand command, CancellationToken cancellationToken);
}
