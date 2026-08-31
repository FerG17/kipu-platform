namespace Kipu.Platform.Suppliers.Interfaces.Rest.Resources;

public record SupplierPaymentPlanResource(int Id, int PurchaseOrderId, int BusinessId, int TotalInstallments,
    int PaidInstallments, bool IsFullyPaid, bool IsCancelled, IReadOnlyCollection<SupplierInstallmentPaymentResource> Payments,
    IReadOnlyCollection<SupplierPaymentInstallmentResource> Installments);

public record SupplierInstallmentPaymentResource(int Id, decimal Amount, DateTimeOffset PaidAt, int PaidByUserId,
    bool IsReversed, DateTimeOffset? ReversedAt, int? ReversedByUserId);

public record SupplierPaymentInstallmentResource(int Id, int Number, DateOnly DueDate, decimal Amount, bool IsPaid);
