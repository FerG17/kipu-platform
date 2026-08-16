using Kipu.Platform.Dashboard.Domain.Model.Commands;
using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Shared.Application.Model;

namespace Kipu.Platform.Dashboard.Application.CommandServices;

public interface IReportCommandService
{
    Task<Result<Report>> Handle(GenerateReportCommand command, CancellationToken cancellationToken);
}
