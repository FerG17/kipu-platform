using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Alerts.Interfaces.Acl;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Interfaces.Rest.Resources;
using Kipu.Platform.Products.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Products.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/batches")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Product expiration batches")]
public class BatchesController(
    IBatchQueryService batchQueryService,
    IInventoryCommandService inventoryCommandService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory,
    IBusinessClock businessClock,
    IAlertsContextFacade alertsContextFacade)
    : ControllerBase
{
    /// <summary>Lists all batches for the business (used to compute business-wide expiration alerts), or for a single product when ?productId= is given.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List batches", OperationId = "GetBatches")]
    public async Task<IActionResult> GetBatches([FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var thresholdDays = await alertsContextFacade.GetExpirationThresholdDays(businessId.Value, cancellationToken);
        var batches = productId.HasValue
            ? await batchQueryService.Handle(new GetAllBatchesByProductIdQuery(productId.Value), cancellationToken)
            : await batchQueryService.Handle(new GetAllBatchesByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(batches.Select(batch => BatchResourceFromEntityAssembler.ToResourceFromEntity(batch, businessClock.Today, thresholdDays)));
    }

    /// <summary>Creates a batch, or updates the product's existing ACTIVE batch in place — see CreateOrUpdateBatchCommand.</summary>
    [HttpPost]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Create or update a product's active batch", OperationId = "CreateOrUpdateBatch")]
    public async Task<IActionResult> CreateOrUpdateBatch([FromBody] CreateOrUpdateBatchResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateOrUpdateBatchCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await inventoryCommandService.Handle(command, cancellationToken);
        var thresholdDays = await alertsContextFacade.GetExpirationThresholdDays(businessId.Value, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            batch => Ok(BatchResourceFromEntityAssembler.ToResourceFromEntity(batch, businessClock.Today, thresholdDays)));
    }

    /// <summary>
    ///     Retires a batch whose goods left the shelf (thrown out, returned).
    ///     This is what stops an expired batch from alerting forever, and it
    ///     closes the alerts it left open in the same action — a domain
    ///     action, so POST rather than PATCH.
    /// </summary>
    [HttpPost("{id:int}/discard")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Discard a batch (goods left the shelf)", OperationId = "DiscardBatch")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The batch was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The batch was already discarded")]
    public async Task<IActionResult> DiscardBatch([FromRoute] int id, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var result = await inventoryCommandService.Handle(new DiscardBatchCommand(id), cancellationToken);
        var thresholdDays = await alertsContextFacade.GetExpirationThresholdDays(businessId.Value, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            batch => Ok(BatchResourceFromEntityAssembler.ToResourceFromEntity(batch, businessClock.Today, thresholdDays)));
    }
}
