namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

/// <summary>
///     IsFullyPaid is only ever true for a Credit sale whose PaymentPlan has
///     collected every installment — always false for a Paid/Cancelled sale
///     (paid at checkout, nothing left to track) or a Credit sale with no
///     plan attached yet. Lets the frontend show "Completada" for a credit
///     sale that's actually done, instead of "A crédito" forever (X5 #5) —
///     see SaleResourceFromEntityAssembler.
/// </summary>
public record SaleResource(
    int Id,
    int BusinessId,
    int? CustomerId,
    string Status,
    decimal TotalAmount,
    string PaymentMethod,
    DateTimeOffset Date,
    string Description,
    string Currency,
    IReadOnlyCollection<SaleDetailResource> Details,
    bool IsFullyPaid);
