namespace Kipu.Platform.Sales.Interfaces.Rest.Resources;

public record PaymentPlanResource(int Id, int SaleId, int BusinessId, int TotalInstallments, int PaidInstallments,
    bool IsFullyPaid, bool IsCancelled, IReadOnlyCollection<InstallmentPaymentResource> Payments,
    IReadOnlyCollection<PaymentInstallmentResource> Installments);

public record InstallmentPaymentResource(int Id, decimal Amount, DateTimeOffset PaidAt, int PaidByUserId,
    bool IsReversed, DateTimeOffset? ReversedAt, int? ReversedByUserId);

public record PaymentInstallmentResource(int Id, int Number, DateOnly DueDate, decimal Amount, bool IsPaid);
