using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Kipu.Platform.Iam.Domain.Model.Entities;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Kipu.Platform.Suppliers.Application.CommandServices;
using Kipu.Platform.Suppliers.Application.QueryServices;
using Kipu.Platform.Suppliers.Domain.Model.Commands;
using Kipu.Platform.Suppliers.Domain.Model.Queries;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;
using Kipu.Platform.Suppliers.Interfaces.Rest.Transform;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Suppliers.Interfaces.Rest;

/// <summary>
///     Credit purchase tracking (X6 #12) — how many installments a purchase
///     order is split into and how many have been paid. Deliberately
///     separate from PurchasesController: attaching a payment plan never
///     touches how the underlying order was created, totaled, or received.
///     Mirrors Sales' PaymentPlansController (X6 #7) exactly.
/// </summary>
[Authorize(RoleNames.Admin, RoleNames.Warehouse)]
[ApiController]
[Route("api/v1/supplier-payment-plans")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Credit purchase order installment plans")]
public class SupplierPaymentPlansController(
    ISupplierPaymentPlanCommandService supplierPaymentPlanCommandService,
    ISupplierPaymentPlanQueryService supplierPaymentPlanQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Gets the payment plan for a specific purchase order (fully paid or not) — 404 if that order has no plan.</summary>
    [HttpGet("by-purchase-order/{purchaseOrderId:int}")]
    [SwaggerOperation(Summary = "Get the payment plan for a purchase order", OperationId = "GetSupplierPaymentPlanByPurchaseOrder")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "That purchase order has no payment plan")]
    public async Task<IActionResult> GetSupplierPaymentPlanByPurchaseOrder([FromRoute] int purchaseOrderId, CancellationToken cancellationToken)
    {
        var plan = await supplierPaymentPlanQueryService.Handle(new GetSupplierPaymentPlanByPurchaseOrderIdQuery(purchaseOrderId), cancellationToken);
        if (plan == null || plan.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan));
    }

    /// <summary>Pending (not fully paid) plans — for the whole business, or for one supplier's orders with ?supplierId=.</summary>
    [HttpGet("pending")]
    [SwaggerOperation(Summary = "List pending supplier payment plans", OperationId = "GetPendingSupplierPaymentPlans")]
    public async Task<IActionResult> GetPendingSupplierPaymentPlans([FromQuery] int? supplierId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var plans = supplierId.HasValue
            ? await supplierPaymentPlanQueryService.Handle(new GetPendingSupplierPaymentPlansBySupplierIdQuery(supplierId.Value), cancellationToken)
            : await supplierPaymentPlanQueryService.Handle(new GetPendingSupplierPaymentPlansByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(plans.Select(SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Attaches a payment plan to an already-existing purchase order — at most one plan per order.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create a payment plan for a purchase order", OperationId = "CreateSupplierPaymentPlan")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "That purchase order already has a payment plan")]
    public async Task<IActionResult> CreateSupplierPaymentPlan([FromBody] CreateSupplierPaymentPlanResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreateSupplierPaymentPlanCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await supplierPaymentPlanCommandService.Handle(command, cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => CreatedAtAction(nameof(GetSupplierPaymentPlanByPurchaseOrder), new { purchaseOrderId = plan.PurchaseOrderId },
                SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    /// <summary>Registers the payment of one installment — domain action, not a field update, so POST not PATCH.</summary>
    [HttpPost("{id:int}/register-payment")]
    [SwaggerOperation(Summary = "Register the payment of one installment", OperationId = "RegisterSupplierInstallmentPayment")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "All installments were already paid")]
    public async Task<IActionResult> RegisterSupplierInstallmentPayment([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId;
        if (userId == null) return Unauthorized();

        var result = await supplierPaymentPlanCommandService.Handle(new RegisterSupplierInstallmentPaymentCommand(id, userId.Value),
            cancellationToken);
        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => Ok(SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    /// <summary>Undoes the most recently registered payment — Admin only, same reasoning as Sales' equivalent endpoint.</summary>
    [Authorize(RoleNames.Admin)]
    [HttpPost("{id:int}/revert-last-payment")]
    [SwaggerOperation(Summary = "Revert the most recently registered payment", OperationId = "RevertSupplierInstallmentPayment")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "There is no payment left to revert")]
    public async Task<IActionResult> RevertSupplierInstallmentPayment([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId;
        if (userId == null) return Unauthorized();

        var result = await supplierPaymentPlanCommandService.Handle(new RevertSupplierInstallmentPaymentCommand(id, userId.Value),
            cancellationToken);
        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => Ok(SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    /// <summary>Edits an unpaid cuota's date/amount — allowed even when other cuotas in the plan are already paid.</summary>
    [HttpPatch("{id:int}/installments/{installmentId:int}")]
    [SwaggerOperation(Summary = "Edit an unpaid cuota's date/amount", OperationId = "UpdateSupplierPaymentInstallment")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "That cuota was already paid, or the new total doesn't match the order")]
    public async Task<IActionResult> UpdateSupplierPaymentInstallment([FromRoute] int id, [FromRoute] int installmentId,
        [FromBody] UpdateSupplierPaymentInstallmentResource resource, CancellationToken cancellationToken)
    {
        var command = UpdateSupplierPaymentInstallmentCommandFromResourceAssembler.ToCommandFromResource(id, installmentId, resource);
        var result = await supplierPaymentPlanCommandService.Handle(command, cancellationToken);

        return SuppliersActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => Ok(SupplierPaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }
}
