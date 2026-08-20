using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Interfaces.Rest.Resources;
using Kipu.Platform.Products.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Domain.Model.Queries;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Shared.Interfaces.Rest.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Products.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/products")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Product catalog")]
public class ProductsController(
    IProductCommandService productCommandService,
    IProductQueryService productQueryService,
    IInventoryCommandService inventoryCommandService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List products of the current business (paginated)", OperationId = "GetProducts")]
    public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] bool includeInactive,
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var pageRequest = PageRequest.Create(page, pageSize);
        var result = await productQueryService.Handle(
            new GetProductsPageByBusinessIdQuery(businessId.Value, category, includeInactive, pageRequest), cancellationToken);
        var supplierIdsByProduct = await productQueryService.Handle(
            new GetProductSupplierIdsByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(new PagedResource<ProductResource>(result.Items.Select(product => ProductResourceFromEntityAssembler.ToResourceFromEntity(
                product, supplierIdsByProduct.GetValueOrDefault(product.Id, []))),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get a product by its id", OperationId = "GetProductById")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The product was not found")]
    public async Task<IActionResult> GetProductById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(id), cancellationToken);
        if (product == null || product.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        var supplierIds = await productQueryService.Handle(new GetProductSupplierIdsQuery(id), cancellationToken);
        return Ok(ProductResourceFromEntityAssembler.ToResourceFromEntity(product, supplierIds));
    }

    [HttpPost]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Create a product", OperationId = "CreateProduct")]
    [SwaggerResponse(StatusCodes.Status201Created, "The product was created", typeof(ProductResource))]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductResource resource, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateProductCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await productCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            product => CreatedAtAction(nameof(GetProductById), new { id = product.Id },
                ProductResourceFromEntityAssembler.ToResourceFromEntity(product, resource.SupplierIds ?? [])));
    }

    [HttpPatch("{id:int}")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Update a product", OperationId = "UpdateProduct")]
    public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] UpdateProductResource resource,
        CancellationToken cancellationToken)
    {
        var command = UpdateProductCommandFromResourceAssembler.ToCommandFromResource(resource, id);
        var result = await productCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            product => Ok(ProductResourceFromEntityAssembler.ToResourceFromEntity(product, resource.SupplierIds ?? [])));
    }

    /// <summary>Business rule: blocked (409) if the product still has stock in any warehouse.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Delete a product", OperationId = "DeleteProduct")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The product still has stock")]
    public async Task<IActionResult> DeleteProduct([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await productCommandService.Handle(new DeleteProductCommand(id), cancellationToken);
        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }

    /// <summary>Undoes DeleteProduct's soft delete, bringing a discontinued product back into the active catalog.</summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Reactivate a deactivated product", OperationId = "ActivateProduct")]
    public async Task<IActionResult> ActivateProduct([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await productCommandService.Handle(new ActivateProductCommand(id), cancellationToken);
        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => NoContent());
    }

    /// <summary>
    ///     Explicit command endpoint (rather than manipulating /inventories
    ///     directly) — encapsulates "sum if an InventoryItem already exists
    ///     for this (product, warehouse), create otherwise, always record a
    ///     StockMovement".
    /// </summary>
    [HttpPost("{id:int}/stock-intake")]
    [Authorize(RoleNames.Admin, RoleNames.Warehouse)]
    [SwaggerOperation(Summary = "Register a stock intake for a product", OperationId = "RegisterStockIntake")]
    public async Task<IActionResult> RegisterStockIntake([FromRoute] int id, [FromBody] RegisterStockIntakeResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = RegisterStockIntakeCommandFromResourceAssembler.ToCommandFromResource(resource, id, businessId.Value);
        var result = await inventoryCommandService.Handle(command, cancellationToken);

        return ProductActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            item => Ok(InventoryItemResourceFromEntityAssembler.ToResourceFromEntity(item)));
    }
}
