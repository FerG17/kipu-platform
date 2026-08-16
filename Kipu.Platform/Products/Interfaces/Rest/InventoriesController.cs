using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Interfaces.Rest.Resources;
using Kipu.Platform.Products.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Products.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/inventories")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Current stock per product/warehouse")]
public class InventoriesController(
    IInventoryQueryService inventoryQueryService,
    IInventoryCommandService inventoryCommandService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Lists inventory for the current business, or for a single product when ?productId= is given.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List inventory items", OperationId = "GetInventoryItems")]
    public async Task<IActionResult> GetInventoryItems([FromQuery] int? productId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var items = productId.HasValue
            ? await inventoryQueryService.Handle(new GetInventoryByProductIdQuery(productId.Value), cancellationToken)
            : await inventoryQueryService.Handle(new GetInventoryByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(items.Select(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPatch("{productId:int}/minimum-stock")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Update a product's minimum stock threshold", OperationId = "UpdateMinimumStock")]
    public async Task<IActionResult> UpdateMinimumStock([FromRoute] int productId, [FromBody] UpdateMinimumStockResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateMinimumStockCommandFromResourceAssembler.ToCommandFromResource(resource, productId);
        var result = await inventoryCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            item => Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }
}
