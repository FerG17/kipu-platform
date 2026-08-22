using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Dashboard.Application.CommandServices;
using Kipu.Platform.Dashboard.Application.QueryServices;
using Kipu.Platform.Dashboard.Domain.Model.Queries;
using Kipu.Platform.Dashboard.Interfaces.Rest.Resources;
using Kipu.Platform.Dashboard.Interfaces.Rest.Transform;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Dashboard.Interfaces.Rest;

/// <summary>
///     Reports are persisted for history (decided §8.3) — unlike the
///     frontend's current 100%-in-memory generation — but their figures are
///     never snapshotted: both generation and export re-run the same live
///     queries against Sales/Product. Same restriction as DashboardController
///     — financial reporting is owner-only.
/// </summary>
[Authorize(RoleNames.Admin)]
[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Generated report history")]
public class ReportsController(
    IReportCommandService reportCommandService,
    IReportQueryService reportQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List generated reports (history) of the current business", OperationId = "GetReports")]
    public async Task<IActionResult> GetReports(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var reports = await reportQueryService.Handle(new GetAllReportsByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(reports.Select(ReportResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Generate a report (persists its metadata for history)", OperationId = "GenerateReport")]
    public async Task<IActionResult> GenerateReport([FromBody] GenerateReportResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = GenerateReportCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await reportCommandService.Handle(command, cancellationToken);

        return DashboardActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            report => Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(report)));
    }

    /// <summary>The old CSV export was replaced with a real formatted .xlsx workbook — see ExcelReportGenerator.</summary>
    [HttpGet("{id:int}/export/excel")]
    [SwaggerOperation(Summary = "Export a report as a formatted .xlsx workbook, re-running its live query", OperationId = "ExportReportAsExcel")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The report was not found")]
    public async Task<IActionResult> ExportReportAsExcel([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await reportQueryService.ExportReportAsExcel(id, cancellationToken);

        return DashboardActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            excel => File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"report-{id}.xlsx"));
    }

    /// <summary>Only STOCK_MOVEMENTS ("entradas/salidas") reports support PDF today — see ReportQueryService.ExportReportAsPdf.</summary>
    [HttpGet("{id:int}/export/pdf")]
    [SwaggerOperation(Summary = "Export a report as PDF, re-running its live query", OperationId = "ExportReportAsPdf")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The report was not found")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "PDF export isn't supported for this report's type")]
    public async Task<IActionResult> ExportReportAsPdf([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await reportQueryService.ExportReportAsPdf(id, cancellationToken);

        return DashboardActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            pdf => File(pdf, "application/pdf", $"report-{id}.pdf"));
    }
}
