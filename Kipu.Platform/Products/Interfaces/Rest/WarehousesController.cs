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
[Route("api/v1/warehouses")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Warehouses (storage locations) of a business")]
public class WarehousesController(
    IWarehouseCommandService warehouseCommandService,
    IWarehouseQueryService warehouseQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List warehouses of the current business", OperationId = "GetWarehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var warehouses = await warehouseQueryService.Handle(new GetAllWarehousesByBusinessIdQuery(businessId.Value),
            cancellationToken);
        return Ok(warehouses.Select(WarehouseResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a warehouse by its id", OperationId = "GetWarehouseById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The warehouse was not found")]
    public async Task<IActionResult> GetWarehouseById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseQueryService.Handle(new GetWarehouseByIdQuery(id), cancellationToken);
        if (warehouse == null || warehouse.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(WarehouseResourceFromEntityAssembler.ToResourceFromEntity(warehouse));
    }

    [HttpPost]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Create a warehouse", OperationId = "CreateWarehouse")]
    [SwaggerResponse(StatusCodes.Status201Created, "The warehouse was created", typeof(WarehouseResource))]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateWarehouseCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await warehouseCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            warehouse => CreatedAtAction(nameof(GetWarehouseById), new { id = warehouse.Id },
                WarehouseResourceFromEntityAssembler.ToResourceFromEntity(warehouse)));
    }

    [HttpPatch("{id:int}")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Update a warehouse", OperationId = "UpdateWarehouse")]
    public async Task<IActionResult> UpdateWarehouse([FromRoute] int id, [FromBody] UpdateWarehouseResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateWarehouseCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await warehouseCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            warehouse => Ok(WarehouseResourceFromEntityAssembler.ToResourceFromEntity(warehouse)));
    }
}
