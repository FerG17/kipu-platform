using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Application.QueryServices;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Model.Queries;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Iam.Interfaces.Rest.Resources;
using Kipu.Platform.Iam.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Iam.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/businesses")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Business (tenant) profile")]
public class BusinessesController(
    IBusinessQueryService businessQueryService,
    IBusinessCommandService businessCommandService,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a business by its id", OperationId = "GetBusinessById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The business was not found")]
    public async Task<IActionResult> GetBusinessById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var business = await businessQueryService.Handle(new GetBusinessByIdQuery(id), cancellationToken);
        return business == null
            ? NotFound()
            : Ok(BusinessResourceFromEntityAssembler.ToResourceFromEntity(business));
    }

    /// <summary>Updates business profile fields.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(RoleNames.Admin)]
    [SwaggerOperation(Summary = "Update a business", OperationId = "UpdateBusiness")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The business was not found")]
    public async Task<IActionResult> UpdateBusiness([FromRoute] int id, [FromBody] UpdateBusinessResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateBusinessCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await businessCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            business => Ok(BusinessResourceFromEntityAssembler.ToResourceFromEntity(business)));
    }
}
