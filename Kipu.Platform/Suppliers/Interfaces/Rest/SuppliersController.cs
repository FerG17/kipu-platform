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
[Route("api/v1/suppliers")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Suppliers of a business")]
public class SuppliersController(
    ISupplierCommandService supplierCommandService,
    ISupplierQueryService supplierQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Active suppliers only by default (X4 M11) — pass includeInactive=true for the management page, which also needs to show and reactivate deactivated ones.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List suppliers of the current business (paginated)", OperationId = "GetSuppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] bool includeInactive, [FromQuery] int? page,
        [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);
        var result = await supplierQueryService.Handle(
            new GetAllSuppliersByBusinessIdQuery(businessId.Value, pageRequest, includeInactive), cancellationToken);
        return Ok(new PagedResource<SupplierResource>(result.Items.Select(SupplierResourceFromEntityAssembler.ToResourceFromEntity),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a supplier by id", OperationId = "GetSupplierById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The supplier was not found")]
    public async Task<IActionResult> GetSupplierById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var supplier = await supplierQueryService.Handle(new GetSupplierByIdQuery(id), cancellationToken);
        if (supplier == null || supplier.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(SupplierResourceFromEntityAssembler.ToResourceFromEntity(supplier));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a supplier", OperationId = "CreateSupplier")]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateSupplierCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await supplierCommandService.Handle(command, cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            supplier => CreatedAtAction(nameof(GetSupplierById), new { id = supplier.Id },
                SupplierResourceFromEntityAssembler.ToResourceFromEntity(supplier)));
    }

    /// <summary>A plain field update. Use DELETE to deactivate (soft-delete) instead — see below.</summary>
    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update a supplier", OperationId = "UpdateSupplier")]
    public async Task<IActionResult> UpdateSupplier([FromRoute] int id, [FromBody] UpdateSupplierResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateSupplierCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await supplierCommandService.Handle(command, cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            supplier => Ok(SupplierResourceFromEntityAssembler.ToResourceFromEntity(supplier)));
    }

    /// <summary>Soft-delete: flips Status to INACTIVE rather than removing the row (see architecture doc §6.6).</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Deactivate a supplier", OperationId = "DeactivateSupplier")]
    public async Task<IActionResult> DeactivateSupplier([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await supplierCommandService.Handle(new DeactivateSupplierCommand(id), cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            supplier => Ok(SupplierResourceFromEntityAssembler.ToResourceFromEntity(supplier)));
    }

    /// <summary>X4 M11: undoes DeactivateSupplier — there was no way back from it before.</summary>
    [HttpPatch("{id:int}/activate")]
    [SwaggerOperation(Summary = "Reactivate a supplier", OperationId = "ReactivateSupplier")]
    public async Task<IActionResult> ReactivateSupplier([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await supplierCommandService.Handle(new ReactivateSupplierCommand(id), cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            supplier => Ok(SupplierResourceFromEntityAssembler.ToResourceFromEntity(supplier)));
    }
}
