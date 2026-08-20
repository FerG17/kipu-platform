using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Shared.Interfaces.Rest.Resources;
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
    [SwaggerOperation(Summary = "List purchase orders (paginated)", OperationId = "GetPurchaseOrders")]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] int? supplierId, [FromQuery] int? page,
        [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);

        if (supplierId.HasValue)
        {
            // Already bounded by construction (one supplier's own orders), so
            // this branch pages the in-memory result rather than needing a
            // dedicated DB-side query — FindAllBySupplierIdAsync is also used
            // unpaged by SupplierCommandService's deactivation guard, which
            // needs the true full set.
            var supplierOrders = (await purchaseOrderQueryService.Handle(new GetPurchaseOrdersBySupplierIdQuery(supplierId.Value),
                cancellationToken)).ToList();
            var pagedSupplierOrders = supplierOrders.Skip(pageRequest.Skip).Take(pageRequest.PageSize);
            return Ok(new PagedResource<PurchaseOrderResource>(
                pagedSupplierOrders.Select(PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity),
                pageRequest.Page, pageRequest.PageSize, supplierOrders.Count, pageRequest.PageSize <= 0 ? 0
                    : (int)Math.Ceiling(supplierOrders.Count / (double)pageRequest.PageSize)));
        }

        var result = await purchaseOrderQueryService.Handle(new GetAllPurchaseOrdersByBusinessIdQuery(businessId.Value, pageRequest),
            cancellationToken);
        return Ok(new PagedResource<PurchaseOrderResource>(
            result.Items.Select(PurchaseOrderResourceFromEntityAssembler.ToResourceFromEntity),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
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
