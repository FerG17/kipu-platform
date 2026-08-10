using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Products.Application.CommandServices;
using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Interfaces.Rest.Resources;
using Bodega.Platform.Products.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Products.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/batches")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Product expiration batches")]
public class BatchesController(
    IBatchQueryService batchQueryService,
    IInventoryCommandService inventoryCommandService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Lists all batches for the business (used to compute business-wide expiration alerts), or for a single product when ?productId= is given.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List batches", OperationId = "GetBatches")]
    public async Task<IActionResult> GetBatches([FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var batches = productId.HasValue
            ? await batchQueryService.Handle(new GetAllBatchesByProductIdQuery(productId.Value), cancellationToken)
            : await batchQueryService.Handle(new GetAllBatchesByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(batches.Select(BatchResourceFromEntityAssembler.ToResourceFromEntity));
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

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            batch => Ok(BatchResourceFromEntityAssembler.ToResourceFromEntity(batch)));
    }
}
