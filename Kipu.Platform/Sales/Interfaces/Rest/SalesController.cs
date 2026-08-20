using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Sales.Application.CommandServices;
using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Commands;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;
using Kipu.Platform.Sales.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Sales.Interfaces.Rest;

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

    /// <summary>
    ///     Paid sales' totals plus whatever's actually been collected on
    ///     credit sales' installments — never a credit sale's own total (see
    ///     SaleQueryService.Handle(GetTotalRevenueByBusinessIdQuery)).
    ///     Admin+Cashier, unlike DashboardController's Admin-only KPI — a
    ///     cashier's own POS stats bar needs this same figure, and it must
    ///     come from here rather than be recomputed client-side.
    /// </summary>
    [HttpGet("revenue")]
    [SwaggerOperation(Summary = "Total revenue (optional date range)", OperationId = "GetSalesRevenue")]
    public async Task<IActionResult> GetSalesRevenue([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var totalRevenue = await saleQueryService.Handle(
            new GetTotalRevenueByBusinessIdQuery(businessId.Value, dateFrom, dateTo), cancellationToken);
        return Ok(new SalesRevenueResource(totalRevenue));
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

    /// <summary>
    ///     Only meaningful transition today is to CANCELLED. Admin only — it
    ///     reverses stock and revenue for a sale that already happened, and
    ///     is irreversible, so it gets the same override the DELETE on
    ///     CustomersController already has, rather than inheriting the
    ///     class-level Admin+Cashier default.
    /// </summary>
    [Authorize(RoleNames.Admin)]
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
