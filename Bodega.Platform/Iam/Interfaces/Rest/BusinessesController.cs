using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;
using Bodega.Platform.Iam.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Iam.Interfaces.Rest;

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
