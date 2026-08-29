using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Interfaces.Rest.Resources;
using Kipu.Platform.Products.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Interfaces.Rest.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Products.Interfaces.Rest;

/// <summary>Inventory audit trail — not part of a cashier's day-to-day, restricted to the roles that actually move stock.</summary>
[Authorize(RoleNames.Admin, RoleNames.Warehouse)]
[ApiController]
[Route("api/v1/stock-movements")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Append-only stock movement audit trail")]
public class StockMovementsController(
    IStockMovementQueryService stockMovementQueryService,
    ICurrentUserAccessor currentUserAccessor)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List stock movements of the current business (paginated)", OperationId = "GetStockMovements")]
    public async Task<IActionResult> GetStockMovements([FromQuery] int? page, [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);
        var result = await stockMovementQueryService.Handle(
            new GetAllStockMovementsByBusinessIdQuery(businessId.Value, pageRequest), cancellationToken);
        return Ok(new PagedResource<StockMovementResource>(
            result.Items.Select(StockMovementResourceFromEntityAssembler.ToResourceFromEntity),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    /// <summary>
    ///     Backs Kardex — unpaginated (the frontend needs the whole filtered
    ///     set at once to compute a running balance), ascending by default so
    ///     that balance accumulates oldest-first once a product is selected.
    /// </summary>
    [HttpGet("filtered")]
    [SwaggerOperation(Summary = "List stock movements of the current business, filtered and unpaginated (Kardex)", OperationId = "GetFilteredStockMovements")]
    public async Task<IActionResult> GetFilteredStockMovements([FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo,
        [FromQuery] int? productId, [FromQuery] string? category, [FromQuery] bool ascending, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var movements = await stockMovementQueryService.Handle(
            new GetFilteredStockMovementsQuery(businessId.Value, dateFrom, dateTo, productId, category, ascending),
            cancellationToken);
        return Ok(movements.Select(StockMovementResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
