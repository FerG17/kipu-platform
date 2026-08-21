using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.QueryServices;
using Kipu.Platform.Alerts.Domain.Model.Queries;
using Kipu.Platform.Alerts.Interfaces.Rest.Resources;
using Kipu.Platform.Alerts.Interfaces.Rest.Transform;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Alerts.Interfaces.Rest;

/// <summary>Replaces the frontend's decorative "Reglas" tab with a real, configurable-per-business resource (see architecture doc §5.4).</summary>
[Authorize]
[ApiController]
[Route("api/v1/alert-rules")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Configurable alert thresholds per business")]
public class AlertRulesController(
    IAlertRuleCommandService alertRuleCommandService,
    IAlertRuleQueryService alertRuleQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List alert rules of the current business", OperationId = "GetAlertRules")]
    public async Task<IActionResult> GetAlertRules(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var rules = await alertRuleQueryService.Handle(new GetAlertRulesByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(rules.Select(AlertRuleResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Creates or updates the rule for the given AlertType — one rule per (business, type).</summary>
    [HttpPost]
    [Authorize(RoleNames.Admin)]
    [SwaggerOperation(Summary = "Create or update an alert rule", OperationId = "CreateOrUpdateAlertRule")]
    public async Task<IActionResult> CreateOrUpdateAlertRule([FromBody] CreateOrUpdateAlertRuleResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateOrUpdateAlertRuleCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await alertRuleCommandService.Handle(command, cancellationToken);

        return AlertsActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            rule => Ok(AlertRuleResourceFromEntityAssembler.ToResourceFromEntity(rule)));
    }
}
