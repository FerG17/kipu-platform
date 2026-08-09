using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Alerts.Application.CommandServices;
using Bodega.Platform.Alerts.Application.QueryServices;
using Bodega.Platform.Alerts.Domain.Model.Commands;
using Bodega.Platform.Alerts.Domain.Model.Queries;
using Bodega.Platform.Alerts.Interfaces.Rest.Resources;
using Bodega.Platform.Alerts.Interfaces.Rest.Transform;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Alerts.Interfaces.Rest;

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
    [SwaggerOperation(Summary = "List resolved alerts (immutable history) of the current business", OperationId = "GetAlertHistory")]
    public async Task<IActionResult> GetAlertHistory(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var alerts = await alertQueryService.Handle(new GetAlertHistoryByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Manual/technical creation — the normal path is the reactive engine, not this endpoint (see architecture doc §6.5).</summary>
    [HttpPost]
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
    [SwaggerOperation(Summary = "Acknowledge an alert", OperationId = "AcknowledgeAlert")]
    public async Task<IActionResult> AcknowledgeAlert([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new AcknowledgeAlertCommand(id), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }

    /// <summary>Resolving is final — an already-RESOLVED alert stays immutable history (409 on a second attempt).</summary>
    [HttpPost("{id:int}/resolve")]
    [SwaggerOperation(Summary = "Resolve an alert", OperationId = "ResolveAlert")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The alert was already resolved")]
    public async Task<IActionResult> ResolveAlert([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new ResolveAlertCommand(id), cancellationToken);
        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            alert => Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert)));
    }
}
