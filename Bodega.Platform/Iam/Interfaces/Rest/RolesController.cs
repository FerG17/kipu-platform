using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Iam.Interfaces.Rest.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Iam.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/roles")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Fixed role catalog (ADMIN, CASHIER, WAREHOUSE)")]
public class RolesController(IRoleQueryService roleQueryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List all roles", OperationId = "GetAllRoles")]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        var roles = await roleQueryService.Handle(new GetAllRolesQuery(), cancellationToken);
        return Ok(roles.Select(RoleResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
