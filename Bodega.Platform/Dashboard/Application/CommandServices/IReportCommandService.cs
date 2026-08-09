using Bodega.Platform.Dashboard.Domain.Model.Commands;
using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Shared.Application.Model;

namespace Bodega.Platform.Dashboard.Application.CommandServices;

public interface IReportCommandService
{
    Task<Result<Report>> Handle(GenerateReportCommand command, CancellationToken cancellationToken);
}
