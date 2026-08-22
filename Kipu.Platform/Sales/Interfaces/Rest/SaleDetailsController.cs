using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Model.Queries;
using Kipu.Platform.Sales.Interfaces.Rest.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Sales.Interfaces.Rest;

/// <summary>
///     Read-only by design: a sale's lines are always created atomically
///     with CreateSaleCommand (see §6.4 in the architecture doc — "la
///     creación real ocurre atómica con CreateSaleCommand"), so there's no
///     standalone create/delete of a line here.
/// </summary>
[Authorize(RoleNames.Admin, RoleNames.Cashier)]
[ApiController]
[Route("api/v1/sale-details")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Sale lines (read-only — created atomically with the sale)")]
public class SaleDetailsController(ISaleQueryService saleQueryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List the lines of a sale", OperationId = "GetSaleDetails")]
    public async Task<IActionResult> GetSaleDetails([FromQuery] int saleId, CancellationToken cancellationToken)
    {
        var sale = await saleQueryService.Handle(new GetSaleByIdQuery(saleId), cancellationToken);
        if (sale == null) return NotFound();

        var resource = SaleResourceFromEntityAssembler.ToResourceFromEntity(sale);
        return Ok(resource.Details);
    }
}
