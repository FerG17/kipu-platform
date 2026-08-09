using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Dashboard.Application.CommandServices;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Dashboard.Interfaces.Rest.Resources;
using Bodega.Platform.Dashboard.Interfaces.Rest.Transform;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Dashboard.Interfaces.Rest;

/// <summary>
///     Reports are persisted for history (decided §8.3) — unlike the
///     frontend's current 100%-in-memory generation — but their figures are
///     never snapshotted: both generation and export re-run the same live
///     queries against Sales/Product.
/// </summary>
[Authorize]
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

    [HttpGet("{id:int}/export")]
    [SwaggerOperation(Summary = "Export a report as CSV, re-running its live query", OperationId = "ExportReport")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The report was not found")]
    public async Task<IActionResult> ExportReport([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await reportQueryService.ExportReportAsCsv(id, cancellationToken);

        return DashboardActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            csv => File(Encoding.UTF8.GetBytes(csv), "text/csv", $"report-{id}.csv"));
    }
}
