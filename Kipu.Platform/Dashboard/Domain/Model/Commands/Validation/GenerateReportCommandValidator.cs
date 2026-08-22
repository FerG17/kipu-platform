using FluentValidation;
using Kipu.Platform.Dashboard.Domain.Model.Entities;

namespace Kipu.Platform.Dashboard.Domain.Model.Commands.Validation;

public class GenerateReportCommandValidator : AbstractValidator<GenerateReportCommand>
{
    /// <summary>
    ///     The 3 report types the export pipeline actually knows how to
    ///     build (see ReportQueryService.ExportReportAsExcel) — any other
    ///     string used to slip through NotEmpty() unnoticed, get persisted,
    ///     and then silently fall into that switch's Sales default at export
    ///     time instead of ever being rejected.
    /// </summary>
    private static readonly string[] AllowedTypes = [ReportType.Sales, ReportType.Inventory, ReportType.StockMovements];

    public GenerateReportCommandValidator()
    {
        // Mirrors the column in Dashboard's ModelBuilderExtensions (Report entity).
        RuleFor(command => command.Type).NotEmpty().MaximumLength(20)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");
        RuleFor(command => command)
            .Must(command => !command.DateFrom.HasValue || !command.DateTo.HasValue || command.DateFrom <= command.DateTo)
            .WithMessage("DateFrom must be on or before DateTo.");
        // The over-366-days case is checked separately in ReportCommandService,
        // with its own DashboardError.InvalidDateRange — bundled in here it
        // would collapse into the same generic InvalidReportData message as
        // every other failure, which doesn't tell the user why.
    }
}
