using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Sales.Interfaces.Rest.Resources;

namespace Kipu.Platform.Sales.Interfaces.Rest.Transform;

public static class PaymentPlanResourceFromEntityAssembler
{
    public static PaymentPlanResource ToResourceFromEntity(PaymentPlan plan)
    {
        var payments = plan.Payments
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => new InstallmentPaymentResource(payment.Id, payment.Amount, payment.PaidAt,
                payment.PaidByUserId, payment.IsReversed, payment.ReversedAt, payment.ReversedByUserId))
            .ToList();

        var installments = plan.Installments
            .OrderBy(installment => installment.Number)
            .Select(installment => new PaymentInstallmentResource(installment.Id, installment.Number,
                installment.DueDate, installment.Amount, installment.IsPaid))
            .ToList();

        return new PaymentPlanResource(plan.Id, plan.SaleId, plan.BusinessId, plan.TotalInstallments,
            plan.PaidInstallments, plan.IsFullyPaid, plan.IsCancelled, payments, installments);
    }
}
