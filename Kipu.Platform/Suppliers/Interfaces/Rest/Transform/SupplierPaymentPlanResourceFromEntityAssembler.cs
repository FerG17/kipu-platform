using Kipu.Platform.Suppliers.Domain.Model.Entities;
using Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

namespace Kipu.Platform.Suppliers.Interfaces.Rest.Transform;

public static class SupplierPaymentPlanResourceFromEntityAssembler
{
    public static SupplierPaymentPlanResource ToResourceFromEntity(SupplierPaymentPlan plan)
    {
        var payments = plan.Payments
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => new SupplierInstallmentPaymentResource(payment.Id, payment.Amount, payment.PaidAt,
                payment.PaidByUserId, payment.IsReversed, payment.ReversedAt, payment.ReversedByUserId))
            .ToList();

        var installments = plan.Installments
            .OrderBy(installment => installment.Number)
            .Select(installment => new SupplierPaymentInstallmentResource(installment.Id, installment.Number,
                installment.DueDate, installment.Amount, installment.IsPaid))
            .ToList();

        return new SupplierPaymentPlanResource(plan.Id, plan.PurchaseOrderId, plan.BusinessId, plan.TotalInstallments,
            plan.PaidInstallments, plan.IsFullyPaid, plan.IsCancelled, payments, installments);
    }
}
