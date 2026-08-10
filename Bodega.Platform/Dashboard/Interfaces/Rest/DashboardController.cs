using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Model.Queries;
using Bodega.Platform.Dashboard.Interfaces.Rest.Resources;
using Bodega.Platform.Dashboard.Interfaces.Rest.Transform;
using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Shared.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Dashboard.Interfaces.Rest;

/// <summary>
///     Every value here is computed live from Product/Inventory/Sales — never
///     from a stored snapshot (architecture doc §6.8). Business-wide
///     financial/operational overview, restricted to the owner role.
/// </summary>
[Authorize(RoleNames.Admin)]
[ApiController]
[Route("api/v1/dashboard")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Live KPIs and charts — composes Product/Inventory/Sales, no data of its own")]
public class DashboardController(IDashboardQueryService dashboardQueryService, ICurrentUserAccessor currentUserAccessor)
    : ControllerBase
{
    [HttpGet("kpis")]
    [SwaggerOperation(Summary = "The 6 business KPIs, computed live", OperationId = "GetBusinessKpis")]
    public async Task<IActionResult> GetBusinessKpis(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var result = await dashboardQueryService.Handle(new GetBusinessKpisQuery(businessId.Value), cancellationToken);
        return Ok(BusinessKpisResourceFromResultAssembler.ToResourceFromResult(result));
    }

    /// <summary>Defaults to the last 7 days when dateFrom/dateTo are omitted.</summary>
    [HttpGet("sales-by-day")]
    [SwaggerOperation(Summary = "Weekly sales series", OperationId = "GetSalesByDay")]
    public async Task<IActionResult> GetSalesByDay([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var result = await dashboardQueryService.Handle(new GetSalesByDayQuery(businessId.Value, dateFrom, dateTo), cancellationToken);
        return Ok(result.Select(entry => new SalesByDayResource(entry.Date, entry.Total)));
    }

    [HttpGet("top-stock-products")]
    [SwaggerOperation(Summary = "Top products ranked by real current stock (never by quantity sold)", OperationId = "GetTopStockProducts")]
    public async Task<IActionResult> GetTopStockProducts([FromQuery] int count, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var resolvedCount = count > 0 ? count : 5;
        var result = await dashboardQueryService.Handle(new GetTopStockProductsQuery(businessId.Value, resolvedCount), cancellationToken);
        return Ok(result.Select(entry => new TopStockProductResource(entry.ProductId, entry.ProductName, entry.TotalStock)));
    }
}
