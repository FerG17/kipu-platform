using FluentValidation;
using Microsoft.Extensions.Localization;
using Bodega.Platform.Dashboard.Application.CommandServices;
using Bodega.Platform.Dashboard.Domain.Model.Commands;
using Bodega.Platform.Dashboard.Domain.Model.Entities;
using Bodega.Platform.Dashboard.Domain.Model.Errors;
using Bodega.Platform.Dashboard.Domain.Repositories;
using Bodega.Platform.Dashboard.Resources;
using Bodega.Platform.Shared.Application.Model;
using Bodega.Platform.Shared.Domain.Repositories;

namespace Bodega.Platform.Dashboard.Application.Internal.CommandServices;

/// <summary>Persists only the report's metadata (for history) — the figures themselves are always recomputed live, on generation and on export.</summary>
public class ReportCommandService(
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork,
    IValidator<GenerateReportCommand> generateReportValidator,
    IStringLocalizer<DashboardMessages> localizer)
    : IReportCommandService
{
    public async Task<Result<Report>> Handle(GenerateReportCommand command, CancellationToken cancellationToken)
    {
        if (!(await generateReportValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Report>.Failure(DashboardError.InvalidReportData, localizer[nameof(DashboardError.InvalidReportData)]);

        var report = new Report(command.BusinessId, command.Type, command.DateFrom, command.DateTo, command.ProductId,
            command.SupplierId);
        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Report>.Success(report);
    }
}
