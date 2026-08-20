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
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Shared.Interfaces.Rest.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Sales.Interfaces.Rest;

[Authorize(RoleNames.Admin, RoleNames.Cashier)]
[ApiController]
[Route("api/v1/customers")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Customers of a business")]
public class CustomersController(
    ICustomerCommandService customerCommandService,
    ICustomerQueryService customerQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List customers of the current business (paginated)", OperationId = "GetCustomers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);
        var result = await customerQueryService.Handle(new GetAllCustomersByBusinessIdQuery(businessId.Value, pageRequest),
            cancellationToken);
        return Ok(new PagedResource<CustomerResource>(result.Items.Select(CustomerResourceFromEntityAssembler.ToResourceFromEntity),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a customer by id", OperationId = "GetCustomerById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The customer was not found")]
    public async Task<IActionResult> GetCustomerById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var customer = await customerQueryService.Handle(new GetCustomerByIdQuery(id), cancellationToken);
        if (customer == null || customer.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(CustomerResourceFromEntityAssembler.ToResourceFromEntity(customer));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create a customer", OperationId = "CreateCustomer")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateCustomerCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await customerCommandService.Handle(command, cancellationToken);

        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            customer => CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id },
                CustomerResourceFromEntityAssembler.ToResourceFromEntity(customer)));
    }

    [HttpPatch("{id:int}")]
    [SwaggerOperation(Summary = "Update a customer", OperationId = "UpdateCustomer")]
    public async Task<IActionResult> UpdateCustomer([FromRoute] int id, [FromBody] UpdateCustomerResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateCustomerCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await customerCommandService.Handle(command, cancellationToken);

        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            customer => Ok(CustomerResourceFromEntityAssembler.ToResourceFromEntity(customer)));
    }

    /// <summary>Soft delete (I31) — restricted to Admin, unlike the rest of this controller which Cashiers can also use.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(RoleNames.Admin)]
    [SwaggerOperation(Summary = "Deactivate a customer", OperationId = "DeleteCustomer")]
    public async Task<IActionResult> DeleteCustomer([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await customerCommandService.Handle(new DeleteCustomerCommand(id), cancellationToken);
        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }
}
