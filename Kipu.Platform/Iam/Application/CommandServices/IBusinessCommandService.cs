using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Commands;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Iam.Application.CommandServices;

public interface IBusinessCommandService
{
    Task<Result<Business>> Handle(UpdateBusinessCommand command, CancellationToken cancellationToken);
}
