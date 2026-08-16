using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Suppliers.Application.CommandServices;
using Kipu.Platform.Suppliers.Application.QueryServices;
using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Domain.Model.Queries;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;
using Kipu.Platform.Suppliers.Interfaces.Rest.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Suppliers.Interfaces.Rest;

[Authorize(RoleNames.Admin, RoleNames.Warehouse)]
[ApiController]
[Route("api/v1/purchases")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Purchase orders placed with suppliers")]
public class PurchasesController(
    IPurchaseOrderCommandService purchaseOrderCommandService,
    IPurchaseOrderQueryService purchaseOrderQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Lists purchase orders of the current business, or of a single supplier when ?supplierId= is given.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List purchase orders", OperationId = "GetPurchaseOrders")]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] int? supplierId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var orders = supplierId.HasValue
            ? await purchaseOrderQueryService.Handle(new GetPurchaseOrdersBySupplierIdQuery(supplierId.Value), cancellationToken)
            : await purchaseOrderQueryService.Handle(new GetAllPurchaseOrdersByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(orders.Select(PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a purchase order (with its lines) by id", OperationId = "GetPurchaseOrderById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The purchase order was not found")]
    public async Task<IActionResult> GetPurchaseOrderById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderQueryService.Handle(new GetPurchaseOrderByIdQuery(id), cancellationToken);
        if (order == null || order.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity(order));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a purchase order with its lines", OperationId = "CreatePurchaseOrder")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreatePurchaseOrderCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await purchaseOrderCommandService.Handle(command, cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            order => CreatedAtAction(nameof(GetPurchaseOrderById), new { id = order.Id },
                PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity(order)));
    }

    /// <summary>Moving to RECEIVED triggers a real stock intake for every line — see PurchaseOrderCommandService.</summary>
    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update a purchase order's status", OperationId = "UpdatePurchaseOrderStatus")]
    public async Task<IActionResult> UpdatePurchaseOrderStatus([FromRoute] int id,
        [FromBody] UpdatePurchaseOrderStatusResource resource, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderCommandService.Handle(new UpdatePurchaseOrderStatusCommand(id, resource.Status),
            cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            order => Ok(PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity(order)));
    }
}
