using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.QueryServices;
using Kipu.Platform.Alerts.Domain.Model.Commands;
using Kipu.Platform.Alerts.Domain.Model.Queries;
using Kipu.Platform.Alerts.Interfaces.Rest.Resources;
using Kipu.Platform.Alerts.Interfaces.Rest.Transform;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Shared.Interfaces.Rest.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Alerts.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/alerts")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Operational alerts — low stock, out of stock, expiring soon, expired")]
public class AlertsController(
    IAlertCommandService alertCommandService,
    IAlertQueryService alertQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List active alerts of the current business", OperationId = "GetActiveAlerts")]
    public async Task<IActionResult> GetActiveAlerts(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var alerts = await alertQueryService.Handle(new GetActiveAlertsByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("history")]
    [SwaggerOperation(Summary = "List resolved alerts (immutable history) of the current business (paginated)",
        OperationId = "GetAlertHistory")]
    public async Task<IActionResult> GetAlertHistory([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);
        var result = await alertQueryService.Handle(new GetAlertHistoryByBusinessIdQuery(businessId.Value, pageRequest),
            cancellationToken);
        return Ok(new PagedResource<AlertResource>(result.Items.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    /// <summary>Manual/technical creation — the normal path is the reactive engine, not this endpoint (see architecture doc §6.5).</summary>
    [HttpPost]
    [Authorize(RoleNames.Admin)]
    [SwaggerOperation(Summary = "Create a manual/technical alert", OperationId = "CreateAlert")]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateAlertCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await alertCommandService.Handle(command, cancellationToken);

        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    /// <summary>Domain action, not a field replacement — POST, not PATCH (see architecture doc §8.4).</summary>
    [HttpPost("{id:int}/acknowledge")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Acknowledge an alert", OperationId = "AcknowledgeAlert")]
    public async Task<IActionResult> AcknowledgeAlert([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new AcknowledgeAlertCommand(id), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    /// <summary>Resolving is final — an already-RESOLVED alert stays immutable history (409 on a second attempt).</summary>
    [HttpPost("{id:int}/resolve")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Resolve an alert", OperationId = "ResolveAlert")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The alert was already resolved")]
    public async Task<IActionResult> ResolveAlert([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new ResolveAlertCommand(id), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }
}
