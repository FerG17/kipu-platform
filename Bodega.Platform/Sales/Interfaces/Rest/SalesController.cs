using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Sales.Application.CommandServices;
using Bodega.Platform.Sales.Application.QueryServices;
using Bodega.Platform.Sales.Domain.Model.Commands;
using Bodega.Platform.Sales.Domain.Model.Queries;
using Bodega.Platform.Sales.Interfaces.Rest.Resources;
using Bodega.Platform.Sales.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Sales.Interfaces.Rest;

[Authorize(RoleNames.Admin, RoleNames.Cashier)]
[ApiController]
[Route("api/v1/sales")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Point-of-sale transactions")]
public class SalesController(
    ISaleCommandService saleCommandService,
    ISaleQueryService saleQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List sales of the current business (optional date range)", OperationId = "GetSales")]
    public async Task<IActionResult> GetSales([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var sales = await saleQueryService.Handle(new GetAllSalesByBusinessIdQuery(businessId.Value, dateFrom, dateTo),
            cancellationToken);
        return Ok(sales.Select(SaleResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a sale (with its lines) by id", OperationId = "GetSaleById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The sale was not found")]
    public async Task<IActionResult> GetSaleById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var sale = await saleQueryService.Handle(new GetSaleByIdQuery(id), cancellationToken);
        if (sale == null || sale.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(SaleResourceFromEntityAssembler.ToResourceFromEntity(sale));
    }

    /// <summary>Creates and confirms a sale atomically: validates stock per line, persists, decrements stock — todo o nada.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create (and confirm) a sale with its lines", OperationId = "CreateSale")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A line does not have sufficient stock")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateSaleCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await saleCommandService.Handle(command, cancellationToken);

        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => CreatedAtAction(nameof(GetSaleById), new { id = sale.Id },
                SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }

    /// <summary>Only meaningful transition today is to CANCELLED.</summary>
    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update a sale's status (cancel)", OperationId = "UpdateSaleStatus")]
    public async Task<IActionResult> UpdateSaleStatus([FromRoute] int id, [FromBody] UpdateSaleStatusResource resource,
        CancellationToken cancellationToken)
    {
        var result = await saleCommandService.Handle(new UpdateSaleStatusCommand(id, resource.Status), cancellationToken);

        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            sale => Ok(SaleResourceFromEntityAssembler.ToResourceFromEntity(sale)));
    }
}
