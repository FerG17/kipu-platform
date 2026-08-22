using FluentValidation;
using Microsoft.Extensions.Localization;
using Kipu.Platform.Dashboard.Application.CommandServices;
using Kipu.Platform.Dashboard.Domain.Model.Commands;
using Kipu.Platform.Dashboard.Domain.Model.Entities;
using Kipu.Platform.Dashboard.Domain.Model.Errors;
using Kipu.Platform.Dashboard.Domain.Repositories;
using Kipu.Platform.Dashboard.Resources;
using Kipu.Platform.Shared.Application.Model;
using Kipu.Platform.Shared.Domain.Repositories;

namespace Kipu.Platform.Dashboard.Application.Internal.CommandServices;

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
        // Checked ahead of the generic validator so a too-wide range gets its
        // own specific message instead of collapsing into "invalid data —
        // check the type and date range", which doesn't say what's actually
        // wrong.
        if (command.DateFrom.HasValue && command.DateTo.HasValue
            && command.DateTo.Value.DayNumber - command.DateFrom.Value.DayNumber > 366)
        {
            return Result<Report>.Failure(DashboardError.InvalidDateRange, localizer[nameof(DashboardError.InvalidDateRange)]);
        }

        if (!(await generateReportValidator.ValidateAsync(command, cancellationToken)).IsValid)
            return Result<Report>.Failure(DashboardError.InvalidReportData, localizer[nameof(DashboardError.InvalidReportData)]);

        var report = new Report(command.BusinessId, command.Type, command.DateFrom, command.DateTo, command.ProductId,
            command.SupplierId);
        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Report>.Success(report);
    }
}
