using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Model.Commands;
using Bodega.Platform.Iam.Domain.Model.Queries;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Bodega.Platform.Iam.Interfaces.Rest.Resources;
using Bodega.Platform.Iam.Interfaces.Rest.Transform;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Bodega.Platform.Iam.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/users")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("User accounts (team members) of the authenticated business")]
public class UsersController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Lists the team of the authenticated user's business.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "List users of the current business", OperationId = "GetUsers")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var users = await userQueryService.Handle(new GetAllUsersByBusinessIdQuery(businessId.Value), cancellationToken);
        return Ok(users.Select(UserResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a user by its id", OperationId = "GetUserById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The user was not found")]
    public async Task<IActionResult> GetUserById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (user == null || !BelongsToCurrentBusiness(user.BusinessId)) return NotFound();

        return Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user));
    }

    /// <summary>Invites (creates) a new team member for the current business.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Invite a team member", OperationId = "InviteUser")]
    [SwaggerResponse(StatusCodes.Status201Created, "The user was created", typeof(UserResource))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already registered")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = InviteUserCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => CreatedAtAction(nameof(GetUserById), new { id = user.Id },
                UserResourceFromEntityAssembler.ToResourceFromEntity(user)));
    }

    /// <summary>Updates profile fields only (name, last name, phone) — never the password.</summary>
    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update a user's profile", OperationId = "UpdateUserProfile")]
    public async Task<IActionResult> UpdateProfile([FromRoute] int id, [FromBody] UpdateUserProfileResource resource,
        CancellationToken cancellationToken)
    {
        var existing = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (existing == null || !BelongsToCurrentBusiness(existing.BusinessId)) return NotFound();

        var command = UpdateUserProfileCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            user => Ok(UserResourceFromEntityAssembler.ToResourceFromEntity(user)));
    }

    /// <summary>Changes the user's password — requires the current password, separate from UpdateProfile so it can never be silently wiped.</summary>
    [HttpPost("{id:int}/change-password")]
    [SwaggerOperation(Summary = "Change a user's password", OperationId = "ChangePassword")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Current password is incorrect")]
    public async Task<IActionResult> ChangePassword([FromRoute] int id, [FromBody] ChangePasswordResource resource,
        CancellationToken cancellationToken)
    {
        var existing = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (existing == null || !BelongsToCurrentBusiness(existing.BusinessId)) return NotFound();

        var command = ChangePasswordCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Remove a team member", OperationId = "DeleteUser")]
    public async Task<IActionResult> DeleteUser([FromRoute] int id, CancellationToken cancellationToken)
    {
        var existing = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (existing == null || !BelongsToCurrentBusiness(existing.BusinessId)) return NotFound();

        var result = await userCommandService.Handle(new DeleteUserCommand(id), cancellationToken);
        return IamActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }

    /// <summary>
    ///     Basic tenant-isolation guard: a user from one business can't act on
    ///     a user from another business, even by guessing an id. This is a
    ///     stopgap for IAM specifically — the target design (§5.1) is a global
    ///     EF Core query filter by BusinessId once more entities exist.
    /// </summary>
    private bool BelongsToCurrentBusiness(int businessId)
    {
        return currentUserAccessor.CurrentBusinessId == businessId;
    }
}
