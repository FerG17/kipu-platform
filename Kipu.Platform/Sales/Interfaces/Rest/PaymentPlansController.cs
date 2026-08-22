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
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Swashbuckle.AspNetCore.Annotations;

namespace Kipu.Platform.Sales.Interfaces.Rest;

/// <summary>
///     Credit sales tracking — how many installments a sale is split into
///     and how many have been paid. Deliberately separate from
///     SalesController: attaching a payment plan never touches how the
///     underlying Sale was created, totaled, or had its stock decremented.
/// </summary>
[Authorize(RoleNames.Admin, RoleNames.Cashier)]
[ApiController]
[Route("api/v1/payment-plans")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Credit sale installment plans")]
public class PaymentPlansController(
    IPaymentPlanCommandService paymentPlanCommandService,
    IPaymentPlanQueryService paymentPlanQueryService,
    ICurrentUserAccessor currentUserAccessor,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    /// <summary>Gets the payment plan for a specific sale (fully paid or not) — 404 if that sale has no plan.</summary>
    [HttpGet("by-sale/{saleId:int}")]
    [SwaggerOperation(Summary = "Get the payment plan for a sale", OperationId = "GetPaymentPlanBySale")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "That sale has no payment plan")]
    public async Task<IActionResult> GetPaymentPlanBySale([FromRoute] int saleId, CancellationToken cancellationToken)
    {
        var plan = await paymentPlanQueryService.Handle(new GetPaymentPlanBySaleIdQuery(saleId), cancellationToken);
        if (plan == null || plan.BusinessId != currentUserAccessor.CurrentBusinessId) return NotFound();

        return Ok(PaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan));
    }

    /// <summary>Pending (not fully paid) plans — for the whole business, or for one customer's sales with ?customerId=.</summary>
    [HttpGet("pending")]
    [SwaggerOperation(Summary = "List pending payment plans", OperationId = "GetPendingPaymentPlans")]
    public async Task<IActionResult> GetPendingPaymentPlans([FromQuery] int? customerId, CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var plans = customerId.HasValue
            ? await paymentPlanQueryService.Handle(new GetPendingPaymentPlansByCustomerIdQuery(customerId.Value), cancellationToken)
            : await paymentPlanQueryService.Handle(new GetPendingPaymentPlansByBusinessIdQuery(businessId.Value), cancellationToken);

        return Ok(plans.Select(PaymentPlanResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Attaches a payment plan to an already-existing sale — at most one plan per sale.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create a payment plan for a sale", OperationId = "CreatePaymentPlan")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "That sale already has a payment plan")]
    public async Task<IActionResult> CreatePaymentPlan([FromBody] CreatePaymentPlanResource resource,
        CancellationToken cancellationToken)
    {
        var businessId = currentUserAccessor.CurrentBusinessId;
        if (businessId == null) return Unauthorized();

        var command = CreatePaymentPlanCommandFromResourceAssembler.ToCommandFromResource(resource, businessId.Value);
        var result = await paymentPlanCommandService.Handle(command, cancellationToken);

        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => CreatedAtAction(nameof(GetPaymentPlanBySale), new { saleId = plan.SaleId },
                PaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    /// <summary>
    ///     Registers the payment of one installment — domain action, not a
    ///     field update, so POST not PATCH. {id} is the plan's own id (not
    ///     the sale's) — no separate tenant check needed here, the command's
    ///     lookup already goes through the tenant-scoped repository, so a
    ///     plan id from another business resolves to PaymentPlanNotFound.
    /// </summary>
    [HttpPost("{id:int}/register-payment")]
    [SwaggerOperation(Summary = "Register the payment of one installment", OperationId = "RegisterInstallmentPayment")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "All installments were already paid")]
    public async Task<IActionResult> RegisterInstallmentPayment([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId;
        if (userId == null) return Unauthorized();

        var result = await paymentPlanCommandService.Handle(new RegisterInstallmentPaymentCommand(id, userId.Value),
            cancellationToken);
        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => Ok(PaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    /// <summary>
    ///     Undoes the most recently registered payment — Admin only. Same
    ///     reasoning as SalesController's cancel-sale override: an
    ///     irreversible-if-unaudited money action shouldn't be a cashier's
    ///     single click to undo (it still IS undoable, deliberately — see
    ///     InstallmentPayment.Reverse — but only a manager decides that).
    /// </summary>
    [Authorize(RoleNames.Admin)]
    [HttpPost("{id:int}/revert-last-payment")]
    [SwaggerOperation(Summary = "Revert the most recently registered payment", OperationId = "RevertInstallmentPayment")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "There is no payment left to revert")]
    public async Task<IActionResult> RevertInstallmentPayment([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.CurrentUserId;
        if (userId == null) return Unauthorized();

        var result = await paymentPlanCommandService.Handle(new RevertInstallmentPaymentCommand(id, userId.Value),
            cancellationToken);
        return SalesActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            plan => Ok(PaymentPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }
}
