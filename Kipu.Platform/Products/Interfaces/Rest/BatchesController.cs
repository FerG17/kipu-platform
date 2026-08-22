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

    /// <summary>
    ///     Sets/corrects a batch's expiration after the fact — most useful
    ///     right after a purchase order is received, since that intake path
    ///     has no expiration field of its own (X5 feedback #3). PATCH, not
    ///     POST: unlike discard this isn't a one-way domain action, just a
    ///     field correction — and it changes FEFO order for free, since
    ///     sales re-query batches by Expiration on every draw.
    /// </summary>
    [HttpPatch("{id:int}/expiration")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Update a batch's expiration date", OperationId = "UpdateBatchExpiration")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The batch was not found")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The batch was already discarded")]
    public async Task<IActionResult> UpdateBatchExpiration([FromRoute] int id, [FromBody] UpdateBatchExpirationResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = UpdateBatchExpirationCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await inventoryCommandService.Handle(command, cancellationToken);
        var thresholdDays = await alertsContextFacade.GetExpirationThresholdDays(businessId.Value, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            batch => Ok(BatchResourceFromEntityAssembler.ToResourceFromEntity(batch, businessClock.Today, thresholdDays)));
    }
}
