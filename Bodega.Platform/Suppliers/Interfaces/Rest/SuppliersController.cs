using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Bodega.Platform.Suppliers.Application.CommandServices;
using Bodega.Platform.Suppliers.Application.QueryServices;
using Bodega.Platform.Suppliers.Domain.Model.Commands;
using Bodega.Platform.Suppliers.Domain.Model.Queries;
using Bodega.Platform.Suppliers.Interfaces.Rest.Resources;
using Bodega.Platform.Suppliers.Interfaces.Rest.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Suppliers.Interfaces.Rest;

[Authorize]
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
    [HttpGet]
    [SwaggerOperation(Summary = "List suppliers of the current business", OperationId = "GetSuppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var suppliers = await supplierQueryService.Handle(new GetAllSuppliersByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(suppliers.Select(SupplierResourceFromEntityAssembler.ToResourceFromEntity));
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
}
