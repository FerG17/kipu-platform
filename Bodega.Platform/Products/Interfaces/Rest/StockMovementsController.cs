using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Domain.Model.Entities;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Products.Interfaces.Rest;

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
    [SwaggerOperation(Summary = "List stock movements of the current business", OperationId = "GetStockMovements")]
    public async Task<IActionResult> GetStockMovements(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var movements = await stockMovementQueryService.Handle(new GetAllStockMovementsByBusinessIdQuery(businessId.Value),
            cancellationToken);
        return Ok(movements.Select(StockMovementResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
