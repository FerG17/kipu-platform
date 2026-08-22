using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class SaleResourceFromEntityAssembler
{
    /// <summary>
    ///     isFullyPaid defaults to false — correct on its own for a
    ///     freshly-created sale (no PaymentPlan attached yet) or a
    ///     Paid/Cancelled one (irrelevant). Callers that already know a
    ///     sale's PaymentPlan (SalesController, batched via
    ///     IPaymentPlanQueryService) pass the real value in.
    /// </summary>
    public static SaleResource ToResourceFromEntity(Sale sale, bool isFullyPaid = false)
    {
        var details = sale.SaleDetails
            .Select(detail => new SaleDetailResource(detail.Id, detail.SaleId, detail.ProductId, detail.Quantity,
                detail.UnitPrice, detail.Discount, detail.Subtotal))
            .ToList();

        return new SaleResource(sale.Id, sale.BusinessId, sale.CustomerId, sale.Status, sale.TotalAmount,
            sale.PaymentMethod, sale.Date, sale.Description, sale.Currency, details, isFullyPaid);
    }
}
